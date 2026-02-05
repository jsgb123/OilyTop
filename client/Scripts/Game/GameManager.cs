using System.Collections.Generic;
using Godot;
using oily.top.Network;

namespace oily.top.Game
{
    public partial class GameManager : Node
    {
        [Export]
        public PackedScene PlayerScene { get; set; }

        private NetworkClient networkClient;
        private Dictionary<int, PlayerController> players = new Dictionary<int, PlayerController>();
        private Node2D worldNode;

        public override void _Ready()
        {
            base._Ready();

            // 获取网络客户端
            networkClient = GetNode<NetworkClient>("/root/NetworkClient");

            // 如果根下没有 NetworkClient 节点，则在运行时创建一个并加入根节点
            if (networkClient == null)
            {
                GD.Print("/root/NetworkClient 未找到，运行时创建一个实例");
                var nc = new NetworkClient();
                nc.Name = "NetworkClient";
                GetTree().Root.AddChild(nc);
                networkClient = nc;
            }

            // 查找或创建世界节点
            worldNode = GetNode<Node2D>("World");
            if (worldNode == null)
            {
                worldNode = new Node2D();
                worldNode.Name = "World";
                AddChild(worldNode);
            }

            // 订阅网络信号
            if (networkClient != null)
            {
                networkClient.Connect("message_received", new Callable(this, nameof(OnNetworkMessage)));
                networkClient.Connect("connected", new Callable(this, nameof(OnConnected)));
                networkClient.Connect("disconnected", new Callable(this, nameof(OnDisconnected)));
            }

            GD.Print("游戏管理器已初始化");
        }

        public override void _ExitTree()
        {
            base._ExitTree();

            if (networkClient != null)
            {
                networkClient.Disconnect("message_received", new Callable(this, nameof(OnNetworkMessage)));
                networkClient.Disconnect("connected", new Callable(this, nameof(OnConnected)));
                networkClient.Disconnect("disconnected", new Callable(this, nameof(OnDisconnected)));
            }
        }

        private void OnConnected()
        {
            GD.Print("已连接到游戏服务器");
        }

        private void OnDisconnected(string reason)
        {
            GD.Print($"与服务器断开连接: {reason}");

            // 清理所有玩家
            foreach (var player in players.Values)
            {
                player.QueueFree();
            }
            players.Clear();
        }

        private void OnNetworkMessage(string json)
        {
            // 主要消息处理在PlayerController中，这里处理全局消息
            var message = ProtocolMessage.FromJson(json);
            // ignore if cannot parse
            if (message == null)
                return;
        }

        public void UpdatePlayersFromServer(List<PlayerData> serverPlayers)
        {
            if (serverPlayers == null)
                return;

            foreach (var serverPlayer in serverPlayers)
            {
                if (serverPlayer.Id == networkClient?.PlayerId)
                    continue; // 跳过本地玩家

                if (!players.ContainsKey(serverPlayer.Id))
                {
                    // 创建新玩家
                    CreatePlayer(serverPlayer);
                }
                else
                {
                    // 更新现有玩家
                    UpdatePlayerPosition(
                        serverPlayer.Id,
                        new Vector2(serverPlayer.X, serverPlayer.Y),
                        serverPlayer.Direction
                    );
                }
            }

            // 移除不存在的玩家
            var idsToRemove = new List<int>();
            foreach (var playerId in players.Keys)
            {
                bool found = false;
                foreach (var serverPlayer in serverPlayers)
                {
                    if (serverPlayer.Id == playerId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found && playerId != networkClient?.PlayerId)
                {
                    idsToRemove.Add(playerId);
                }
            }

            foreach (var playerId in idsToRemove)
            {
                RemovePlayer(playerId);
            }
        }

        public void UpdatePlayerPosition(int playerId, Vector2 position, float direction)
        {
            if (players.TryGetValue(playerId, out PlayerController player))
            {
                player.Position = position;
                // 这里可以添加平滑移动
            }
        }

        private void CreatePlayer(PlayerData playerData)
        {
            if (PlayerScene == null)
            {
                GD.PrintErr("PlayerScene未设置");
                return;
            }

            var playerInstance = PlayerScene.Instantiate<PlayerController>();
            if (playerInstance == null)
            {
                GD.PrintErr("无法实例化玩家场景");
                return;
            }

            playerInstance.PlayerId = playerData.Id;
            playerInstance.PlayerName = playerData.Name;
            playerInstance.Position = new Vector2(playerData.X, playerData.Y);

            worldNode.AddChild(playerInstance);
            players[playerData.Id] = playerInstance;

            GD.Print($"创建玩家: {playerData.Name} (ID: {playerData.Id})");
        }

        private void RemovePlayer(int playerId)
        {
            if (players.TryGetValue(playerId, out PlayerController player))
            {
                player.QueueFree();
                players.Remove(playerId);
                GD.Print($"移除玩家: ID={playerId}");
            }
        }

        public PlayerController GetLocalPlayer()
        {
            if (
                networkClient != null
                && players.TryGetValue(networkClient.PlayerId, out PlayerController player)
            )
            {
                return player;
            }
            return null;
        }
    }
}
