using Godot;
using oily.top.Network;

namespace oily.top.UI
{
    public partial class LoginUI : Control
    {
        private int loginCount = 0;

        [Export]
        private LineEdit serverAddressInput;

        [Export]
        private LineEdit playerNameInput;

        [Export]
        private Button connectButton;

        [Export]
        private Label statusLabel;

        private NetworkClient networkClient;
        private bool isSwitchingScene = false; // 检测是否在游戏场景

        public override void _Ready()
        {
            GD.Print("LoginUI _Ready called");
            base._Ready();

            // 获取节点引用
            if (serverAddressInput == null)
                serverAddressInput = GetNode<LineEdit>("VBoxContainer/ServerAddressInput");
            if (playerNameInput == null)
                playerNameInput = GetNode<LineEdit>("VBoxContainer/PlayerNameInput");
            if (connectButton == null)
                connectButton = GetNode<Button>("VBoxContainer/ConnectButton");
            if (statusLabel == null)
                statusLabel = GetNode<Label>("VBoxContainer/StatusLabel");

            // 获取网络客户端
            networkClient = GetNode<NetworkClient>("/root/NetworkClient");

            // 连接信号
            connectButton.Pressed += OnConnectButtonPressed;

            // 订阅网络信号
            if (networkClient != null)
            {
                GD.Print("订阅网络客户端信号");
                networkClient.Connect(
                    NetworkClient.SignalName.Connected,
                    new Callable(this, nameof(OnConnected))
                );
                networkClient.Connect(
                    NetworkClient.SignalName.Disconnected,
                    new Callable(this, nameof(OnDisconnected))
                );
                networkClient.Connect(
                    NetworkClient.SignalName.Error,
                    new Callable(this, nameof(OnError))
                );
            }
            else
            {
                GD.PrintErr("无法找到 NetworkClient 节点！");
            }

            // 设置默认值
            serverAddressInput.Text = "localhost:8080";
            playerNameInput.Text = $"玩家{new System.Random().Next(1000, 9999)}";

            UpdateUI();
            GD.Print("LoginUI _Ready completed");
        }

        public override void _ExitTree()
        {
            base._ExitTree();

            if (networkClient != null)
            {
                networkClient.Disconnect(
                    NetworkClient.SignalName.Connected,
                    new Callable(this, nameof(OnConnected))
                );
                networkClient.Disconnect(
                    NetworkClient.SignalName.Disconnected,
                    new Callable(this, nameof(OnDisconnected))
                );
                networkClient.Disconnect(
                    NetworkClient.SignalName.Error,
                    new Callable(this, nameof(OnError))
                );
            }
            GD.Print("LoginUI _ExitTree completed");
        }

        private void UpdateUI()
        {
            bool isConnected = networkClient?.IsConnected ?? false;

            serverAddressInput.Editable = !isConnected;
            playerNameInput.Editable = !isConnected;
            connectButton.Disabled = isConnected;

            if (isConnected)
            {
                connectButton.Text = "已连接";
            }
            else
            {
                connectButton.Text = "连接";
            }
        }

        private void OnConnectButtonPressed()
        {
            connectButton.Disabled = true;
            string serverAddress = serverAddressInput.Text.Trim();
            string playerName = playerNameInput.Text.Trim();

            if (string.IsNullOrEmpty(serverAddress))
            {
                statusLabel.Text = "请输入服务器地址";
                statusLabel.Modulate = Colors.Red;
                connectButton.Disabled = false;
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                statusLabel.Text = "请输入玩家名称";
                statusLabel.Modulate = Colors.Red;
                connectButton.Disabled = false;
                return;
            }

            statusLabel.Text = "正在连接...";
            statusLabel.Modulate = Colors.Yellow;
            loginCount++;
            GD.Print($"连接尝试次数: {loginCount}");
            // 连接到服务器
            networkClient?.ConnectToServer(serverAddress, playerName);
        }

        private void OnConnected()
        {
            if (isSwitchingScene)
            {
                return;
            }
            isSwitchingScene = true;
            GD.Print("连接成功回调");
            statusLabel.Text = "连接成功！";
            statusLabel.Modulate = Colors.Green;

            UpdateUI();

            // 延迟切换到游戏场景
            CallDeferred(nameof(SwitchToGameScene));
        }

        private void OnDisconnected(string reason)
        {
            statusLabel.Text = $"连接断开: {reason}";
            statusLabel.Modulate = Colors.Red;

            UpdateUI();
        }

        private void OnError(string error)
        {
            statusLabel.Text = $"错误: {error}";
            statusLabel.Modulate = Colors.Red;
            UpdateUI();
        }

        private void SwitchToGameScene()
        {
            // 切换到游戏场景
            if (GetTree().CurrentScene.Name != "GameScene")
            {
                GetTree().ChangeSceneToFile("res://Scenes/GameScene.tscn");
            }
        }
    }
}
