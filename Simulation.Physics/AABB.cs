using System.Numerics;

namespace Simulation.Physics
{
    public readonly record struct AABB(Vector3 Start, Vector3 End, Vector3 Center)
    {
        public Projection Project(Vector3 axis)
        {
            float r = Math.Abs(Vector3.Dot(End - Center, axis.Abs()));
            float s = Vector3.Dot(axis, Center);

            return new Projection
            {
                Minimum = s - r,
                Maximum = s + r,
            };
        }

        public static AABB FromPoints(Vector3 start, Vector3 end)
        {
            return new AABB
            {
                Start = start,
                End = end,
                Center = (start + end) / 2
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

            return AABB.FromPoints(start.Value, end.Value);
        }
    }
}
