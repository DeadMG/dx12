using Data;
using System.Numerics;

namespace Simulation
{
    public class Camera
    {
        public Vector3 Position { get; set; } = new Vector3();
        public Quaternion Orientation { get; set; } = Quaternion.Identity;
        public float Fov { get; set; } = 90;

        public Matrix4x4 ViewProjectionMatrix(ScreenSize size)
        {
            var viewMatrix = Matrix4x4.CreateLookAtLeftHanded(Position, Position + Vector3.Transform(new Vector3(0, 0, 1), Orientation), Vector3.Transform(new Vector3(0, 1, 0), Orientation));
            var projMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Fov.ToRadians(), (float)size.Width / size.Height, 1f, float.PositiveInfinity);

            return viewMatrix * projMatrix;
        }

        public Vector3 Unproject(Vector3 clip, ScreenSize size)
        {
            if (!Matrix4x4.Invert(ViewProjectionMatrix(size), out var unprojection))
            {
                return new Vector3(); // TODO: Better
            }

            return Vector4.Transform(new Vector4(clip, 1), unprojection).PerspectiveDivide();
        }

        public Ray Unproject(Vector2 clip, ScreenSize size)
        {
            if (!Matrix4x4.Invert(ViewProjectionMatrix(size), out var unprojection))
            {
                return null; // TODO: Better
            }

            return new Ray(
                Vector4.Transform(new Vector4(clip.X, clip.Y, 0, 1), unprojection).PerspectiveDivide(),
                Vector4.Transform(new Vector4(clip.X, clip.Y, 1, 1), unprojection).PerspectiveDivide());
        }

        public Frustum Unproject(Vector2 p1, Vector2 p2, ScreenSize size)
        {
            if (!Matrix4x4.Invert(ViewProjectionMatrix(size), out var unprojection))
            {
                return null; // TODO: Better
            }

            // We are LH; but the two points may be supplied in any order.
            // Convert them to LH.

            // These should be first close, then far, clockwise from bottom left
            return new Frustum(
                Vector4.Transform(new Vector4(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), 0, 1), unprojection).PerspectiveDivide(),
                Vector4.Transform(new Vector4(Math.Min(p1.X, p2.X), Math.Max(p1.Y, p2.Y), 0, 1), unprojection).PerspectiveDivide(),
                Vector4.Transform(new Vector4(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y), 0, 1), unprojection).PerspectiveDivide(),
                Vector4.Transform(new Vector4(Math.Max(p1.X, p2.X), Math.Min(p1.Y, p2.Y), 0, 1), unprojection).PerspectiveDivide(),
                Vector4.Transform(new Vector4(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), 1, 1), unprojection).PerspectiveDivide(),
                Vector4.Transform(new Vector4(Math.Min(p1.X, p2.X), Math.Max(p1.Y, p2.Y), 1, 1), unprojection).PerspectiveDivide(),
                Vector4.Transform(new Vector4(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y), 1, 1), unprojection).PerspectiveDivide(),
                Vector4.Transform(new Vector4(Math.Max(p1.X, p2.X), Math.Min(p1.Y, p2.Y), 1, 1), unprojection).PerspectiveDivide());
        }
    }
}
