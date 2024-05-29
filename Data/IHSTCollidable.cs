using System.Numerics;

namespace Data
{
    public interface IHSTCollidable
    {
        public Vector3[] UniqueFaceNormals { get; }
        public Vector3[] UniqueEdgeDirections { get; }
        public Projection Project(Vector3 axis);
    }
}
