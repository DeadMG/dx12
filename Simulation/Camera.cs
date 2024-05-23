using Data;
using System.Numerics;

namespace Simulation
{
    public class Camera
    {
        public Vector3 Position { get; set; } = new Vector3();
        public Quaternion Orientation { get; set; } = new Quaternion();
        public float Fov { get; set; } = 90;

        public Matrix4x4 ViewProjectionMatrix(int width, int height)
        {
            var viewMatrix = Matrix4x4.CreateLookAtLeftHanded(Position, Position + Vector3.Transform(new Vector3(0, 0, 1), Orientation), Vector3.Transform(new Vector3(0, 1, 0), Orientation));
            var projMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Fov.ToRadians(), (float)width / height, 1f, float.PositiveInfinity);

            return viewMatrix * projMatrix;
        }
    }
}
