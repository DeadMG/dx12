using Simulation.Physics;
using System.Numerics;

namespace Simulation
{
    public class Unit : IBoundable
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
                boundingBox = AABB.FromVertices(Blueprint.Mesh.Vertices.Select(v => Vector3.Transform(v.Position, WorldMatrix)));
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
