using Godot;
using oily.top.Game;
using oily.top.Network;

namespace oily.top.UI
{
    public partial class GameUI : Control
    {
        [Export]
        private Label playerInfoLabel;

        [Export]
        private Label serverStatusLabel;

        [Export]
        private Label playerCountLabel;

        [Export]
        private Label fpsLabel;

        [Export]
        private Button disconnectButton;

        private NetworkClient networkClient;
        private GameManager gameManager;

        private float updateTimer = 0f;
        private const float UpdateInterval = 0.5f;

        public override void _Ready()
        {
            base._Ready();

            // 获取节点引用
            if (playerInfoLabel == null)
                playerInfoLabel = GetNode<Label>("HBoxContainer/PlayerInfoLabel");
            if (serverStatusLabel == null)
                serverStatusLabel = GetNode<Label>("HBoxContainer/ServerStatusLabel");
            if (playerCountLabel == null)
                playerCountLabel = GetNode<Label>("HBoxContainer/PlayerCountLabel");
            if (fpsLabel == null)
                fpsLabel = GetNode<Label>("HBoxContainer/FPSLabel");
            if (disconnectButton == null)
                disconnectButton = GetNode<Button>("HBoxContainer/DisconnectButton");

            // 获取网络客户端和游戏管理器
            networkClient = GetNode<NetworkClient>("/root/NetworkClient");
            gameManager = GetNode<GameManager>("/root/GameManager");

            // 连接信号
            disconnectButton.Pressed += OnDisconnectButtonPressed;

            UpdateUI();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            updateTimer += (float)delta;
            if (updateTimer >= UpdateInterval)
            {
                UpdateUI();
                updateTimer = 0f;
            }

            // 显示FPS
            fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
        }

        private void UpdateUI()
        {
            // 玩家信息
            if (networkClient != null)
            {
                playerInfoLabel.Text = $"玩家: ID={networkClient.PlayerId}";

                if (networkClient.IsConnected)
                {
                    serverStatusLabel.Text = "已连接";
                    serverStatusLabel.Modulate = Colors.Green;
                }
                else
                {
                    serverStatusLabel.Text = "离线";
                    serverStatusLabel.Modulate = Colors.Red;
                }
            }

            // 控制说明
            var helpText = GetNode<Label>("HelpText");
            if (helpText != null)
            {
                helpText.Text = "控制: WASD/方向键移动, 空格发送消息, ESC断开连接";
            }
        }

        private void OnDisconnectButtonPressed()
        {
            if (networkClient != null)
            {
                networkClient.Disconnect();
            }

            // 返回登录界面
            GetTree().ChangeSceneToFile("res://Scenes/LoginScene.tscn");
        }

        // 处理ESC键
        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                if (keyEvent.Keycode == Key.Escape)
                {
                    OnDisconnectButtonPressed();
                }
            }
        }
    }
}
