using System.Numerics;

namespace Data
{
    public class Frustum : IHSTCollidable
    {
        public Frustum(Vector3 bottomLeftClose,
            Vector3 topLeftClose,
            Vector3 topRightClose,
            Vector3 bottomRightClose,
            Vector3 bottomLeftFar,
            Vector3 topLeftFar,
            Vector3 topRightFar,
            Vector3 bottomRightFar)
        {
            Points = new Vector3[]
            {
                bottomLeftClose,
                topLeftClose,
                topRightClose,
                bottomRightClose,

                bottomLeftFar,
                topLeftFar,
                topRightFar,
                bottomRightFar,
            };

            UniqueFaceNormals = new Vector3[]
            {
                Plane.CreateFromVertices(bottomLeftClose, bottomRightClose, topLeftClose).Normal, // Close plane
                Plane.CreateFromVertices(bottomLeftClose, bottomLeftFar, topLeftFar).Normal, // Left plane
                Plane.CreateFromVertices(bottomLeftClose, bottomLeftFar, bottomRightClose).Normal, // Bottom plane
                Plane.CreateFromVertices(bottomRightFar, bottomRightClose, topRightFar).Normal, // Right plane
                Plane.CreateFromVertices(topRightFar, topRightClose, topLeftClose).Normal, // Top plane
            };

            UniqueEdgeDirections = new Vector3[]
            {
                bottomLeftClose - bottomRightClose,
                bottomLeftClose - topLeftClose,
                bottomLeftFar - bottomLeftClose,
                bottomRightFar - bottomRightClose,
                topLeftFar - topLeftClose,
                topRightFar - topRightClose,
            };
        }

        public Vector3[] Points { get; private init; }
        public Vector3[] UniqueFaceNormals { get; private init; }
        public Vector3[] UniqueEdgeDirections { get; private init; }

        public Projection Project(Vector3 axis)
        {
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;

            foreach (var point in Points)
            {
                var d = Vector3.Dot(axis, point);
                min = Math.Min(min, d);
                max = Math.Max(max, d);
            }

            return new Projection
            {
                Maximum = max,
                Minimum = min,
            };
        }
    }
}
