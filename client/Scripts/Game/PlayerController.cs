using Godot;
using oily.top.Network;

namespace oily.top.Game
{
    public partial class PlayerController : Node2D
    {
        [Export]
        public int PlayerId { get; set; } = 0;

        [Export]
        public string PlayerName { get; set; } = "玩家";

        private Player player;
        private NetworkClient networkClient;
        private Label nameLabel;
        private Sprite2D playerSprite;
        private Line2D directionLine;

        private float moveTimer = 0f;
        private const float MoveUpdateInterval = 0.1f; // 每100ms发送一次位置更新

        public override void _Ready()
        {
            base._Ready();

            // 创建玩家实例
            player = new Player(PlayerId, PlayerName);

            // 获取网络客户端
            networkClient = GetNode<NetworkClient>("/root/NetworkClient");
            if (networkClient == null)
            {
                GD.PrintErr("找不到NetworkClient节点");
                return;
            }

            // 创建视觉元素
            CreateVisualElements();

            // 订阅网络信号
            networkClient.Connect("message_received", new Callable(this, nameof(OnNetworkMessage)));

            GD.Print($"玩家控制器已初始化: {PlayerName} (ID: {PlayerId})");
        }

        public override void _ExitTree()
        {
            base._ExitTree();

            if (networkClient != null)
            {
                networkClient.Disconnect("message_received", new Callable(this, nameof(OnNetworkMessage)));
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (player == null)
                return;

            // 更新玩家位置
            player.Update((float)delta);
            Position = player.Position;

            // 处理本地玩家输入
            if (PlayerId == networkClient?.PlayerId)
            {
                HandleInput((float)delta);
            }

            // 更新视觉元素
            UpdateVisualElements();
        }

        private void CreateVisualElements()
        {
            // 玩家精灵（圆形）
            playerSprite = new Sprite2D();
            var texture = new CircleShape2D();
            // 创建简单的圆形纹理
            var image = Image.CreateEmpty(64, 64, false, Image.Format.Rgbaf);
            image.Fill(player.Color);

            var imageTexture = ImageTexture.CreateFromImage(image);
            playerSprite.Texture = imageTexture;
            playerSprite.Centered = true;
            AddChild(playerSprite);

            // 方向指示线
            directionLine = new Line2D();
            directionLine.Width = 3.0f;
            directionLine.DefaultColor = Colors.Yellow;
            directionLine.AddPoint(new Vector2(0, 0));
            directionLine.AddPoint(new Vector2(30, 0));
            AddChild(directionLine);

            // 名字标签
            nameLabel = new Label();
            nameLabel.Text = PlayerName;
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            nameLabel.Position = new Vector2(0, -40);
            AddChild(nameLabel);
        }

        private void UpdateVisualElements()
        {
            if (directionLine != null)
            {
                // 更新方向线
                float angle = Mathf.DegToRad(player.Direction);
                Vector2 endPoint = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 30;
                directionLine.SetPointPosition(1, endPoint);
            }

            if (nameLabel != null && nameLabel.Text != PlayerName)
            {
                nameLabel.Text = PlayerName;
            }
        }

        private void HandleInput(float delta)
        {
            Vector2 input = Vector2.Zero;

            if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
                input.Y -= 1;
            if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
                input.Y += 1;
            if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
                input.X -= 1;
            if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
                input.X += 1;

            if (input != Vector2.Zero)
            {
                input = input.Normalized();
                Vector2 newPosition = player.Position + input * player.Speed * delta;

                // 边界检查
                var viewport = GetViewportRect();
                newPosition.X = Mathf.Clamp(newPosition.X, 0, viewport.Size.X);
                newPosition.Y = Mathf.Clamp(newPosition.Y, 0, viewport.Size.Y);

                player.MoveTo(newPosition);

                // 发送位置更新
                moveTimer += delta;
                if (moveTimer >= MoveUpdateInterval)
                {
                    SendPositionUpdate();
                    moveTimer = 0f;
                }
            }
            else
            {
                moveTimer = MoveUpdateInterval; // 强制发送最后一次位置
            }

            // 测试聊天
            if (Input.IsActionJustPressed("ui_accept"))
            {
                networkClient.SendChatMessage("Hello from Godot!");
            }
        }

        private void SendPositionUpdate()
        {
            if (networkClient != null && networkClient.IsConnected)
            {
                networkClient.SendPlayerMove(player.Position, player.Direction);
            }
        }

        private void OnNetworkMessage(string json)
        {
            var message = ProtocolMessage.FromJson(json);
            if (message == null)
                return;

            switch (message.Type)
            {
                case MessageType.MSG_CONNECT_RESPONSE:
                    HandleConnectResponse(message);
                    break;

                case MessageType.MSG_WORLD_STATE:
                    HandleWorldState(message);
                    break;

                case MessageType.MSG_PLAYER_MOVE:
                    HandlePlayerMove(message);
                    break;

                case MessageType.MSG_PLAYER_JOIN:
                    HandlePlayerJoin(message);
                    break;

                case MessageType.MSG_PLAYER_LEAVE:
                    HandlePlayerLeave(message);
                    break;
            }
        }

        private void HandleConnectResponse(ProtocolMessage message)
        {
            var response = System.Text.Json.JsonSerializer.Deserialize<ConnectResponse>(message.Data.GetRawText());

            if (response != null && PlayerId == 0) // 只有本地玩家需要处理
            {
                PlayerId = response.PlayerId;
                player.Id = response.PlayerId;
                player.SetPositionImmediate(new Vector2(response.X, response.Y));

                GD.Print($"玩家位置已更新: ({response.X}, {response.Y})");
            }
        }

        private void HandleWorldState(ProtocolMessage message)
        {
            var worldState = System.Text.Json.JsonSerializer.Deserialize<WorldState>(message.Data.GetRawText());

            if (worldState != null)
            {
                GD.Print($"收到世界状态，玩家数量: {worldState.Players.Count}");

                // 通知GameManager处理
                var gameManager = GetNode<GameManager>("/root/GameManager");
                if (gameManager != null)
                {
                    gameManager.UpdatePlayersFromServer(worldState.Players);
                }
            }
        }

        private void HandlePlayerMove(ProtocolMessage message)
        {
            var playerMove = System.Text.Json.JsonSerializer.Deserialize<PlayerMove>(message.Data.GetRawText());

            if (playerMove != null && playerMove.PlayerId != PlayerId)
            {
                // 更新其他玩家位置
                var gameManager = GetNode<GameManager>("/root/GameManager");
                if (gameManager != null)
                {
                    gameManager.UpdatePlayerPosition(
                        playerMove.PlayerId,
                        new Vector2(playerMove.X, playerMove.Y),
                        playerMove.Direction
                    );
                }
            }
        }

        private void HandlePlayerJoin(ProtocolMessage message)
        {
            GD.Print("有新玩家加入");
        }

        private void HandlePlayerLeave(ProtocolMessage message)
        {
            GD.Print("有玩家离开");
        }
    }
}
