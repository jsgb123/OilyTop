using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace oily.top.Network
{
    // 与Java服务器完全匹配的消息协议
    public class ProtocolMessage
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }

        public ProtocolMessage() { }

        public ProtocolMessage(int type, object data)
        {
            Type = type;
            // 使用 System.Text.Json 序列化
            Data = JsonSerializer.SerializeToElement(data);
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static ProtocolMessage FromJson(string json)
        {
            return JsonSerializer.Deserialize<ProtocolMessage>(json);
        }
    }

    // 消息类型（与Java服务器一致）
    public static class MessageType
    {
        public const int MSG_CONNECT_REQUEST = 1;
        public const int MSG_CONNECT_RESPONSE = 2;
        public const int MSG_PLAYER_MOVE = 3;
        public const int MSG_PLAYER_JOIN = 4;
        public const int MSG_PLAYER_LEAVE = 5;
        public const int MSG_WORLD_STATE = 6;
        public const int MSG_CHAT_MESSAGE = 7;
        public const int MSG_HEARTBEAT = 99;
    }

    // 连接请求
    public class ConnectRequest
    {
        [JsonPropertyName("playerName")]
        public string PlayerName { get; set; }

        public ConnectRequest() { }

        public ConnectRequest(string playerName)
        {
            PlayerName = playerName;
        }
    }

    // 连接响应
    public class ConnectResponse
    {
        [JsonPropertyName("playerId")]
        public int PlayerId { get; set; }

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        public ConnectResponse() { }

        public ConnectResponse(int playerId, float x, float y)
        {
            PlayerId = playerId;
            X = x;
            Y = y;
        }
    }

    // 玩家移动
    public class PlayerMove
    {
        [JsonPropertyName("playerId")]
        public int PlayerId { get; set; }

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("direction")]
        public float Direction { get; set; }

        public PlayerMove() { }

        public PlayerMove(int playerId, float x, float y, float direction)
        {
            PlayerId = playerId;
            X = x;
            Y = y;
            Direction = direction;
        }
    }

    // 玩家数据
    public class PlayerData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("direction")]
        public float Direction { get; set; }

        public PlayerData() { }

        public PlayerData(int id, string name, float x, float y, float direction)
        {
            Id = id;
            Name = name;
            X = x;
            Y = y;
            Direction = direction;
        }
    }

    // 世界状态
    public class WorldState
    {
        [JsonPropertyName("players")]
        public List<PlayerData> Players { get; set; }

        public WorldState() { }

        public WorldState(List<PlayerData> players)
        {
            Players = players;
        }
    }

    // 聊天消息
    public class ChatMessage
    {
        [JsonPropertyName("playerId")]
        public int PlayerId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        public ChatMessage() { }

        public ChatMessage(int playerId, string message)
        {
            PlayerId = playerId;
            Message = message;
        }
    }
}
