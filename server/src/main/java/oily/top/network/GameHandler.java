package oily.top.network;

import com.fasterxml.jackson.core.JsonProcessingException;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.SimpleChannelInboundHandler;
import io.netty.handler.codec.http.websocketx.TextWebSocketFrame;
import io.netty.handler.codec.http.websocketx.WebSocketFrame;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.HashMap;
import java.util.Map;
import oily.top.game.Player;
import oily.top.game.World;

public class GameHandler extends SimpleChannelInboundHandler<WebSocketFrame> {
    private static final Logger logger = LoggerFactory.getLogger(GameHandler.class);

    private static final Map<String, Integer> playerSessions = new HashMap<>();
    private final World world = World.getInstance();

    @Override
    protected void channelRead0(ChannelHandlerContext ctx, WebSocketFrame frame) throws Exception {
        if (frame instanceof TextWebSocketFrame) {
            String request = ((TextWebSocketFrame) frame).text();
            handleMessage(ctx, request);
        } else {
            logger.warn("不支持的WebSocket帧类型: {}", frame.getClass().getName());
        }
    }

    private void handleMessage(ChannelHandlerContext ctx, String message) {
        try {
            Protocol.Message msg = Protocol.deserialize(message);

            switch (msg.type) {
                case Protocol.MSG_CONNECT_REQUEST:
                    handleConnectRequest(ctx, (Map<String, Object>) msg.data);
                    break;

                case Protocol.MSG_PLAYER_MOVE:
                    handlePlayerMove(ctx, (Map<String, Object>) msg.data);
                    break;

                case Protocol.MSG_CHAT_MESSAGE:
                    handleChatMessage(ctx, (Map<String, Object>) msg.data);
                    break;
                case Protocol.MSG_HEARTBEAT:
                    handleHeartbeatMessage(ctx, (Map<String, Object>) msg.data);
                    break;
                default:
                    logger.warn("未知消息类型: {}", msg.type);
            }

        } catch (Exception e) {
            logger.error("处理消息失败: {}", message, e);
            sendError(ctx, "消息格式错误");
        }
    }

    private void handleHeartbeatMessage(ChannelHandlerContext ctx, Map<String, Object> data) {

        Integer playerId = (Integer) data.get("playerId");
        Number timestamp = (Number) data.get("timestamp");
        logger.info("收到心跳消息: playerId={}, timestamp={}", playerId, timestamp);

        // 回复心跳确认消息
        try {
            Map<String, Object> responseData = new HashMap<>();
            responseData.put("playerId", playerId);
            responseData.put("timestamp", timestamp);
            Protocol.Message response = new Protocol.Message(Protocol.MSG_HEARTBEAT, responseData);
            ctx.writeAndFlush(new TextWebSocketFrame(Protocol.serialize(response)));
        } catch (JsonProcessingException e) {
            logger.error("心跳响应序列化失败", e);
        }
    }

    private void handleConnectRequest(ChannelHandlerContext ctx, Map<String, Object> data) {
        String playerName = (String) data.get("playerName");
        if (playerName == null || playerName.trim().isEmpty()) {
            playerName = "玩家" + System.currentTimeMillis() % 1000;
        }

        // 创建新玩家
        Player player = world.createPlayer(playerName);
        String sessionId = ctx.channel().id().asShortText();
        playerSessions.put(sessionId, player.getId());

        logger.info("玩家连接: {} (ID: {}), 会话: {}", playerName, player.getId(), sessionId);

        // 发送连接响应
        try {
            Protocol.Message response = Protocol.createConnectResponse(
                    player.getId(), player.getX(), player.getY());
            ctx.writeAndFlush(new TextWebSocketFrame(Protocol.serialize(response)));

            // 广播玩家加入
            broadcastPlayerJoin(player);

            // 发送当前世界状态
            sendWorldState(ctx, player.getId());

        } catch (JsonProcessingException e) {
            logger.error("序列化响应失败", e);
        }
    }

    private void handlePlayerMove(ChannelHandlerContext ctx, Map<String, Object> data) {
        Integer playerId = (Integer) data.get("playerId");
        Number x = (Number) data.get("x");
        Number y = (Number) data.get("y");
        Number direction = (Number) data.get("direction");

        if (playerId == null || x == null || y == null) {
            return;
        }

        Player player = world.getPlayer(playerId);
        if (player != null) {
            player.setX(x.floatValue());
            player.setY(y.floatValue());
            if (direction != null) {
                player.setDirection(direction.floatValue());
            }

            logger.debug("玩家移动: ID={}, 位置=({}, {})", playerId, x, y);

            // 广播移动信息给其他玩家
            broadcastPlayerMove(player);
        }
    }

    private void handleChatMessage(ChannelHandlerContext ctx, Map<String, Object> data) {
        Integer playerId = (Integer) data.get("playerId");
        String message = (String) data.get("message");

        if (playerId != null && message != null) {
            Player player = world.getPlayer(playerId);
            if (player != null) {
                logger.info("聊天: {}: {}", player.getName(), message);
                // 这里可以广播聊天消息
            }
        }
    }

    private void broadcastPlayerJoin(Player player) {
        // 在实际项目中，这里会广播给所有在线玩家
        logger.info("广播玩家加入: {}", player.getName());
    }

    private void broadcastPlayerMove(Player player) {
        // 在实际项目中，这里会广播给附近玩家
        logger.debug("广播玩家移动: {} ({}, {})",
                player.getName(), player.getX(), player.getY());
    }

    private void sendWorldState(ChannelHandlerContext ctx, int excludePlayerId) {
        try {
            Protocol.Message worldState = Protocol.createWorldState(world.getAllPlayersData());
            ctx.writeAndFlush(new TextWebSocketFrame(Protocol.serialize(worldState)));
        } catch (JsonProcessingException e) {
            logger.error("发送世界状态失败", e);
        }
    }

    private void sendError(ChannelHandlerContext ctx, String error) {
        try {
            Map<String, Object> errorData = new HashMap<>();
            errorData.put("error", error);
            Protocol.Message msg = new Protocol.Message(999, errorData);
            ctx.writeAndFlush(new TextWebSocketFrame(Protocol.serialize(msg)));
        } catch (JsonProcessingException e) {
            logger.error("发送错误消息失败", e);
        }
    }

    @Override
    public void channelInactive(ChannelHandlerContext ctx) throws Exception {
        String sessionId = ctx.channel().id().asShortText();
        Integer playerId = playerSessions.remove(sessionId);

        if (playerId != null) {
            Player player = world.removePlayer(playerId);
            if (player != null) {
                logger.info("玩家断开连接: {} (ID: {})", player.getName(), playerId);
                broadcastPlayerLeave(playerId);
            }
        }

        super.channelInactive(ctx);
    }

    private void broadcastPlayerLeave(int playerId) {
        // 在实际项目中，这里会广播给所有在线玩家
        logger.info("广播玩家离开: ID={}", playerId);
    }

    @Override
    public void exceptionCaught(ChannelHandlerContext ctx, Throwable cause) {
        logger.error("连接异常", cause);
        ctx.close();
    }
}