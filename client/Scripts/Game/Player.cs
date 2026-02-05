using System;
using Godot;

namespace oily.top.Game
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Vector2 Position { get; set; }
        public float Direction { get; set; }
        public Color Color { get; set; }
        public float Speed { get; set; } = 200.0f;

        // 用于平滑移动
        private Vector2 targetPosition;
        private bool isMoving = false;

        public Player(int id, string name)
        {
            Id = id;
            Name = name;
            Position = new Vector2(400, 300);
            targetPosition = Position;

            // 随机颜色
            var random = new Random();
            Color = new Color(
                random.Next(100, 255) / 255.0f,
                random.Next(100, 255) / 255.0f,
                random.Next(100, 255) / 255.0f
            );
        }

        public void Update(float delta)
        {
            // 平滑移动到目标位置
            if (isMoving)
            {
                Position = Position.MoveToward(targetPosition, Speed * delta);

                if (Position.DistanceTo(targetPosition) < 1.0f)
                {
                    Position = targetPosition;
                    isMoving = false;
                }
            }
        }

        public void MoveTo(Vector2 newPosition)
        {
            targetPosition = newPosition;
            isMoving = true;

            // 计算方向
            if (newPosition != Position)
            {
                Vector2 direction = (newPosition - Position).Normalized();
                Direction = Mathf.RadToDeg(Mathf.Atan2(direction.Y, direction.X));
            }
        }

        public void SetPositionImmediate(Vector2 position)
        {
            Position = position;
            targetPosition = position;
            isMoving = false;
        }
    }
}
