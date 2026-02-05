package oily.top.network;

import io.netty.bootstrap.ServerBootstrap;
import io.netty.channel.ChannelFuture;
import io.netty.channel.ChannelInitializer;
import io.netty.channel.ChannelOption;
import io.netty.channel.EventLoopGroup;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.SocketChannel;
import io.netty.channel.socket.nio.NioServerSocketChannel;
import io.netty.handler.codec.http.HttpObjectAggregator;
import io.netty.handler.codec.http.HttpServerCodec;
import io.netty.handler.codec.http.websocketx.WebSocketServerProtocolHandler;
import io.netty.handler.logging.LogLevel;
import io.netty.handler.logging.LoggingHandler;
import io.netty.handler.stream.ChunkedWriteHandler;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class GameServer {
    private static final Logger logger = LoggerFactory.getLogger(GameServer.class);
    
    private final int port;
    private EventLoopGroup bossGroup;
    private EventLoopGroup workerGroup;
    
    public GameServer(int port) {
        this.port = port;
    }
    
    public void start() throws InterruptedException {
        bossGroup = new NioEventLoopGroup(1);
        workerGroup = new NioEventLoopGroup();
        
        try {
            ServerBootstrap b = new ServerBootstrap();
            b.group(bossGroup, workerGroup)
             .channel(NioServerSocketChannel.class)
             .handler(new LoggingHandler(LogLevel.INFO))
             .childHandler(new ChannelInitializer<SocketChannel>() {
                 @Override
                 protected void initChannel(SocketChannel ch) throws Exception {
                     ch.pipeline().addLast(
                         new HttpServerCodec(),
                         new ChunkedWriteHandler(),
                         new HttpObjectAggregator(65536),
                         new WebSocketServerProtocolHandler("/ws"),
                         new GameHandler()
                     );
                 }
             })
             .option(ChannelOption.SO_BACKLOG, 128)
             .childOption(ChannelOption.SO_KEEPALIVE, true);
            
            ChannelFuture f = b.bind(port).sync();
            logger.info("WebSocket服务器已启动在端口: {}", port);
            
            f.channel().closeFuture().sync();
            
        } finally {
            stop();
        }
    }
    
    public void stop() {
        if (workerGroup != null) {
            workerGroup.shutdownGracefully();
        }
        if (bossGroup != null) {
            bossGroup.shutdownGracefully();
        }
        logger.info("服务器已停止");
    }
    
    public void awaitTermination() throws InterruptedException {
        Thread.currentThread().join();
    }
}