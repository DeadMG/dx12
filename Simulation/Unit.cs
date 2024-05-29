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

                float? minX = null;
                float? maxX = null;
                float? minY = null;
                float? maxY = null;
                float? minZ = null;
                float? maxZ = null;

                foreach (var vert in Blueprint.Mesh.Vertices.Select(v => Vector3.Transform(v.Position, WorldMatrix)))
                {
                    minX = minX == null ? vert.X : Math.Min(minX.Value, vert.X);
                    maxX = maxX == null ? vert.X : Math.Max(maxX.Value, vert.X);
                    minY = minY == null ? vert.Y : Math.Min(minY.Value, vert.Y);
                    maxY = maxY == null ? vert.Y : Math.Max(minX.Value, vert.Y);
                    minZ = minZ == null ? vert.Z : Math.Min(minZ.Value, vert.Z);
                    maxZ = maxZ == null ? vert.Z : Math.Max(maxZ.Value, vert.Z);
                }

                boundingBox = new AABB
                {
                    Start = new Vector3(minX.Value, minY.Value, minZ.Value),
                    End = new Vector3(maxX.Value, maxY.Value, maxZ.Value)
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
