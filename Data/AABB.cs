using System.Numerics;

namespace Data
{
    public struct AABB : IHSTCollidable
    {
        // Start should be lowest x, y z; end is highest.
        public required Vector3 Start { get; init; }
        public required Vector3 End { get; init; }

        public Vector3 Center() => (Start + End) / 2;
        public Vector3[] UniqueFaceNormals => normals;
        public Vector3[] UniqueEdgeDirections => normals;

        public Projection Project(Vector3 axis)
        {
            var center = Center();
            float r = Math.Abs(Vector3.Dot(End - center, axis.Abs()));
            float s = Vector3.Dot(axis, center);

            return new Projection
            {
                Minimum = s - r,
                Maximum = s + r,
            };
        }

        public static AABB FromVertices(IEnumerable<Vector3> vertices)
        {
            Vector3? start = null;
            Vector3? end = null;

            foreach (var vert in vertices)
            {
                if (start == null)
                {
                    start = vert;
                    end = vert;
                    continue;
                }

                start = start.Value.Min(vert);
                end = end.Value.Max(vert);
            }

            return new AABB
            {
                Start = start.Value,
                End = end.Value
            };
        }

        private static readonly Vector3[] normals =
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1)
        };
    }
}
