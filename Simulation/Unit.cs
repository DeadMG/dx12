using Data;
using System.Numerics;

namespace Simulation
{
    public class Unit
    {
        private Vector3 position;
        private Quaternion orientation;

        private readonly Player player;
        private readonly Blueprint blueprint;

        public Unit(Player player, Blueprint blueprint, Vector3 position, Quaternion orientation)
        {
            this.position = position;
            this.orientation = orientation;
            this.player = player;
            this.blueprint = blueprint;
        }

        public float Velocity = 0;
        public Vector3 Position { get { return position; } set { position = value; Clear(); } }
        public Quaternion Orientation { get { return orientation; } set { orientation = value; Clear(); } }

        public Player Player => player;
        public Blueprint Blueprint => blueprint;
        public Queue<IOrder> Orders { get; } = new Queue<IOrder>();

        private AABB? boundingBox;
        public AABB BoundingBox
        {
            get
            {
                if (boundingBox != null) return boundingBox.Value;

                Vector3? min = null;
                Vector3? max = null;

                foreach (var pos in Blueprint.Mesh.Vertices.Select(v => Vector3.Transform(v.Position, WorldMatrix)))
                {
                    min = min == null ? pos : min.Value.Min(pos);
                    max = max == null ? pos : max.Value.Max(pos);
                }

                boundingBox = new AABB
                {
                    Start = min.Value,
                    End = max.Value
                };
                return boundingBox.Value;
            }
        }

        private Matrix4x4? worldMatrix;
        public Matrix4x4 WorldMatrix
        {
            get
            {
                if (worldMatrix.HasValue) return worldMatrix.Value;
                worldMatrix = Matrix4x4.CreateFromQuaternion(Orientation) * Matrix4x4.CreateTranslation(Position);
                return worldMatrix.Value;
            }
        }

        private void Clear()
        {
            worldMatrix = null;
            boundingBox = null;
        }
    }
}
