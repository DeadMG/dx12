using System.Numerics;

namespace Simulation.Physics
{
    public class Collision
    {
        public static bool Intersect(AABB volume, Vector3 position)
        {
            if (position.X < volume.Start.X) return false;
            if (position.X > volume.End.X) return false;
            if (position.Y < volume.Start.Y) return false;
            if (position.Y > volume.End.Y) return false;
            if (position.Z < volume.Start.Z) return false;
            if (position.Z > volume.End.Z) return false;
            return true;
        }

        public static bool IsContainedWithin(AABB item, AABB container)
        {
            if (!Intersect(container, item.Start)) return false;
            if (!Intersect(container, item.End)) return false;

            return true;
        }

        public static bool Intersect(AABB volume, Ray ray)
        {
            var tx1 = (volume.Start.X - ray.Start.X) * ray.Inverse.X;
            var tx2 = (volume.End.X - ray.Start.X) * ray.Inverse.X;

            var tmin = Math.Min(tx1, tx2);
            var tmax = Math.Max(tx1, tx2);

            var ty1 = (volume.Start.Y - ray.Start.Y) * ray.Inverse.Y;
            var ty2 = (volume.End.Y - ray.Start.Y) * ray.Inverse.Y;

            tmin = Math.Max(tmin, Math.Min(ty1, ty2));
            tmax = Math.Min(tmax, Math.Max(ty1, ty2));

            var tz1 = (volume.Start.Z - ray.Start.Z) * ray.Inverse.Z;
            var tz2 = (volume.End.Z - ray.Start.Z) * ray.Inverse.Z;

            tmin = Math.Max(tmin, Math.Min(tz1, tz2));
            tmax = Math.Min(tmax, Math.Max(tz1, tz2));

            return tmax >= Math.Max(0.0, tmin);
        }

        // This is a full intersect check, not a quick cull.
        public static bool Intersect(AABB left, Frustum right)
        {
            foreach (var axis in aabbNormals)
            {
                if (Separated(axis, left, right)) return false;
            }

            foreach (var axis in right.UniqueFaceNormals)
            {
                if (Separated(axis, left, right)) return false;
            }

            foreach (var leftEdge in aabbNormals)
            foreach (var rightEdge in right.UniqueEdgeDirections)
            {
                if (Separated(Vector3.Cross(-leftEdge, rightEdge), left, right)) return false;
            }

            return true;
        }

        private static bool Separated(Vector3 axis, AABB left, Frustum right)
        {
            var leftProj = left.Project(axis);
            var rightProj = right.Project(axis);

            if (leftProj.Maximum < rightProj.Minimum || rightProj.Maximum < leftProj.Minimum)
                return true;

            return false;
        }

        private static readonly Vector3[] aabbNormals =
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1)
        };
    }
}
