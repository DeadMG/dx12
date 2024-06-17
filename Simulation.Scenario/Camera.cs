using Data.Space;
using System.Numerics;

namespace Simulation
{
    public class Camera
    {
        private ScreenSize size;
        private float fov;
        private Vector3 position;
        private Quaternion orientation;

        public Camera(ScreenSize size, Vector3 position, Quaternion orientation, float fov)
        {
            this.size = size;
            this.position = position;
            this.orientation = orientation;
            this.fov = fov;

            CreateProjectionMatrices();
        }

        public Vector3 Position { get => position; set { position = value; CreateProjectionMatrices(); } }
        public Quaternion Orientation { get => orientation; set { orientation = value; CreateProjectionMatrices(); } }
        public float Fov { get => fov; set { fov = value; CreateProjectionMatrices(); } }
        public ScreenSize ScreenSize => size;

        public Matrix4x4 ViewProjection { get; private set; }
        public Matrix4x4 InvViewProjection { get; private set; }

        public void Resize(ScreenSize newSize)
        {
            this.size = newSize;

            CreateProjectionMatrices();
        }

        private void CreateProjectionMatrices()
        {
            var viewMatrix = Matrix4x4.CreateLookAtLeftHanded(Position, Position + Vector3.Transform(new Vector3(0, 0, 1), Orientation), Vector3.Transform(new Vector3(0, 1, 0), Orientation));
            var projMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Fov.ToRadians(), (float)size.Width / size.Height, 1f, 1000);
            ViewProjection = viewMatrix * projMatrix;

            if (Matrix4x4.Invert(ViewProjection, out var unprojection))
            {
                InvViewProjection = unprojection;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
