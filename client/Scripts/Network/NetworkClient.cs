using System;
using System.Collections.Concurrent;
using Godot;

namespace oily.top.Network
{
    public partial class NetworkClient : Node
    {
        private WebSocketPeer webSocket;
        private readonly ConcurrentQueue<string> receiveQueue = new();
        private readonly ConcurrentQueue<string> sendQueue = new();
        private Timer connectionTimeoutTimer;
        private Timer heartbeatTimer;
        private bool isConnectionPending = false;
        private DateTime connectionStartTime;
        private DateTime lastActivityTime;
        private bool wasConnected = false; // 记录之前是否连接成功过
        private int consecutiveHeartbeatFailures = 0; // 心跳失败计数
        private const int MAX_HEARTBEAT_FAILURES = 3; // 最大允许的心跳失败次数

        // Godot 信号定义
        [Signal]
        public delegate void ConnectedEventHandler();

        [Signal]
        public delegate void DisconnectedEventHandler(string reason);

        [Signal]
        public delegate void ErrorEventHandler(string message);

        [Signal]
        public delegate void MessageReceivedEventHandler(string json);

        [Signal]
        public delegate void HeartbeatTimeoutEventHandler();

        public new bool IsConnected => webSocket?.GetReadyState() == WebSocketPeer.State.Open;
        public int PlayerId { get; private set; }

        public override void _Ready()
        {
            base._Ready();

            // 初始化连接超时计时器
            connectionTimeoutTimer = new Timer();
            connectionTimeoutTimer.WaitTime = 5.0; // 5秒连接超时
            connectionTimeoutTimer.OneShot = true; // 只执行一次
            connectionTimeoutTimer.Timeout += OnConnectionTimeout;
            AddChild(connectionTimeoutTimer);

            // 初始化心跳计时器
            heartbeatTimer = new Timer();
            heartbeatTimer.WaitTime = 15; // 每15秒发送心跳
            heartbeatTimer.OneShot = false;
            heartbeatTimer.Timeout += OnHeartbeatTimeout;
            AddChild(heartbeatTimer);
        }

        /// <summary>
        /// 连接超时处理程序
        /// </summary>
        private void OnConnectionTimeout()
        {
            if (isConnectionPending)
            {
                GD.Print("连接超时，服务器可能未启动");
                isConnectionPending = false;
                EmitSignal(SignalName.Error, "连接超时，服务器未响应");
                EmitSignal(SignalName.Disconnected, "连接超时");

                CleanupWebSocket();
            }
        }

        private void CleanupWebSocket()
        {
            webSocket?.Close();
            webSocket = null;
        }

        //定期发心跳
        private void OnHeartbeatTimeout()
        {
            if (IsConnected && wasConnected)
            {
                consecutiveHeartbeatFailures++;
                if (consecutiveHeartbeatFailures >= MAX_HEARTBEAT_FAILURES)
                {
                    GD.Print("心跳超时，强制断开");
                    EmitSignal(SignalName.HeartbeatTimeout, "心跳超时信号");
                    CleanupWebSocket();
                    return;
                }

                SendHeartbeat();
                // 注意：不要在这里更新 lastActivityTime！
            }
        }

        private void SendHeartbeat()
        {
            if (IsConnected)
            {
                var message = new Godot.Collections.Dictionary
                {
                    ["type"] = MessageType.MSG_HEARTBEAT, // 自定义心跳消息类型
                    ["data"] = new Godot.Collections.Dictionary
                    {
                        ["playerId"] = PlayerId,
                        ["timestamp"] = DateTime.Now.Ticks,
                    },
                };
                SendJson(message);
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (webSocket != null)
            {
                webSocket.Poll(); // 轮询连接状态

                // 检查连接状态
                var state = webSocket.GetReadyState();
                CheckConnectionState(state);

                // 接收消息
                ReceiveMessages();

                // 发送消息
                SendMessages();
            }
        }

        private void CheckConnectionState(WebSocketPeer.State state)
        {
            switch (state)
            {
                case WebSocketPeer.State.Open:
                    if (isConnectionPending)
                    {
                        GD.Print("✅ WebSocket连接已建立");
                        isConnectionPending = false;
                        connectionTimeoutTimer.Stop();
                        wasConnected = true;
                        lastActivityTime = DateTime.Now;
                        heartbeatTimer.Start(); // 启动心跳
                        EmitSignal(SignalName.Connected);
                    }
                    else if (wasConnected)
                    {
                        // 检查连接是否超时（长时间无响应）
                        var idleTime = DateTime.Now - lastActivityTime;
                        if (idleTime.TotalSeconds > 60.0) // 60秒无活动
                        {
                            GD.Print($"连接可能已断开，{idleTime.TotalSeconds:F0} 秒无活动");
                            CleanupWebSocket();
                        }
                    }
                    break;

                case WebSocketPeer.State.Closed:
                    if (isConnectionPending)
                    {
                        // 连接过程中被拒绝或服务器关闭
                        GD.Print("❌ 连接失败：服务器未响应或已关闭");
                        isConnectionPending = false;
                        connectionTimeoutTimer.Stop();
                        EmitSignal(SignalName.Error, "无法连接到服务器");
                        EmitSignal(SignalName.Disconnected, "连接失败");
                    }
                    else if (wasConnected)
                    {
                        // 已经连接后被断开
                        GD.Print("🔌 连接已关闭");
                        wasConnected = false;
                        heartbeatTimer.Stop(); // 停止心跳
                        EmitSignal(SignalName.Disconnected, "连接关闭");
                    }
                    webSocket = null; // 清理 WebSocket 实例
                    break;

                case WebSocketPeer.State.Connecting:
                    if (isConnectionPending)
                    {
                        var duration = DateTime.Now - connectionStartTime;
                        GD.Print($"⏳ 正在连接服务器... ({duration.TotalSeconds:F1}秒)");
                    }
                    break;

                case WebSocketPeer.State.Closing:
                    GD.Print("正在关闭连接...");
                    break;
            }
        }

        private void ReceiveMessages()
        {
            while (webSocket.GetAvailablePacketCount() > 0)
            {
                var packet = webSocket.GetPacket();
                if (packet != null && packet.Length > 0)
                {
                    string json = System.Text.Encoding.UTF8.GetString(packet);
                    lastActivityTime = DateTime.Now; // 更新活动时间
                    consecutiveHeartbeatFailures = 0;
                    ProcessJsonMessage(json);
                }
            }
        }

        private void ProcessJsonMessage(string json)
        {
            try
            {
                GD.Print($"收到消息: {json}");
                // 发出原始 JSON 字符串，订阅者在需要时解析
                EmitSignal(SignalName.MessageReceived, json);

                // 另外内部尝试解析连接响应以设置 PlayerId
                var parsed = ProtocolMessage.FromJson(json);
                if (parsed != null && parsed.Type == MessageType.MSG_CONNECT_RESPONSE)
                {
                    var response = System.Text.Json.JsonSerializer.Deserialize<ConnectResponse>(
                        parsed.Data.GetRawText()
                    );
                    if (response != null)
                    {
                        PlayerId = response.PlayerId;
                        GD.Print($"玩家ID设置为: {PlayerId}");
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"处理消息失败: {ex.Message}");
                EmitSignal(SignalName.Error, ex.Message);
            }
        }

        private void SendMessages()
        {
            while (sendQueue.TryDequeue(out string json))
            {
                if (IsConnected)
                {
                    webSocket.SendText(json);
                    lastActivityTime = DateTime.Now; // 更新活动时间
                    GD.Print($"发送消息: {json.Length} 字符");
                }
                else
                {
                    GD.Print($"无法发送消息，连接已断开: {json}");
                }
            }
        }

        public void ConnectToServer(string serverUrl, string playerName)
        {
            if (IsConnected)
            {
                GD.Print("已经连接到服务器");
                return;
            }

            try
            {
                // 重置状态
                Disconnect();

                string url = $"ws://{serverUrl}/ws";
                GD.Print($"正在连接到: {url}");

                // 创建新的 WebSocket 实例
                webSocket = new WebSocketPeer();

                var error = webSocket.ConnectToUrl(url);
                if (error != Godot.Error.Ok)
                {
                    GD.PrintErr($"WebSocket 连接初始化失败: {error}");
                    EmitSignal(SignalName.Error, $"连接失败: {error}");
                    EmitSignal(SignalName.Disconnected, "连接初始化失败");
                    isConnectionPending = false;
                    connectionTimeoutTimer.Stop();
                    webSocket = null;
                    return;
                }

                isConnectionPending = true;
                wasConnected = false;
                connectionStartTime = DateTime.Now;
                connectionTimeoutTimer.Start();

                GD.Print("连接请求已发送，等待响应...");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"连接异常: {ex.Message}");
                EmitSignal(SignalName.Error, ex.Message);
                isConnectionPending = false;
            }
        }

        public void Disconnect()
        {
            if (webSocket != null)
            {
                var state = webSocket.GetReadyState();
                if (state != WebSocketPeer.State.Closed && state != WebSocketPeer.State.Closing)
                {
                    webSocket.Close();
                }
            }

            PlayerId = 0;
            isConnectionPending = false;
            wasConnected = false;
            connectionTimeoutTimer.Stop();
            heartbeatTimer.Stop();

            GD.Print("已断开连接");
        }

        public void SendConnectRequest(string playerName)
        {
            var message = new Godot.Collections.Dictionary
            {
                ["type"] = 1,
                ["data"] = new Godot.Collections.Dictionary { ["playerName"] = playerName },
            };

            SendJson(message);
        }

        private void SendJson(Godot.Collections.Dictionary message)
        {
            try
            {
                string json = Json.Stringify(message);
                sendQueue.Enqueue(json);
                GD.Print($"发送: {json}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建JSON失败: {ex.Message}");
                EmitSignal(SignalName.Error, ex.Message); // 使用 EmitSignal
            }
            lastActivityTime = DateTime.Now;
        }

        // 主动检测方法
        public void TestConnection()
        {
            if (!IsConnected)
            {
                GD.Print("连接未建立");
                return;
            }

            // 发送测试消息
            var testMsg = new Godot.Collections.Dictionary
            {
                ["type"] = 100, // 测试消息类型
                ["data"] = new Godot.Collections.Dictionary
                {
                    ["test"] = "ping",
                    ["timestamp"] = DateTime.Now.Ticks,
                },
            };

            SendJson(testMsg);
            GD.Print("发送连接测试消息");
        }

        public void SendPlayerMove(Vector2 position, float direction)
        {
            if (PlayerId == 0)
            {
                GD.Print("玩家ID为0，无法发送移动");
                return;
            }

            var message = new Godot.Collections.Dictionary
            {
                ["type"] = 3,
                ["data"] = new Godot.Collections.Dictionary
                {
                    ["playerId"] = PlayerId,
                    ["x"] = position.X,
                    ["y"] = position.Y,
                    ["direction"] = direction,
                },
            };

            SendJson(message);
        }

        public void SendChatMessage(string text)
        {
            if (PlayerId == 0)
            {
                GD.Print("玩家ID为0，无法发送聊天");
                return;
            }

            var message = new Godot.Collections.Dictionary
            {
                ["type"] = 7,
                ["data"] = new Godot.Collections.Dictionary
                {
                    ["playerId"] = PlayerId,
                    ["message"] = text,
                },
            };

            SendJson(message);
        }
    }
}
