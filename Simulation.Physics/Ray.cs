using Data.Space;
using System.Numerics;

namespace Simulation.Physics
{
    public readonly record struct Ray(Vector3 Start, Vector3 End, Vector3 Inverse)
    {
        public static Ray FromPoints(Vector3 start, Vector3 end)
        {
            var direction = Vector3.Normalize(end - start);
            var inverse = new Vector3(1 / direction.X, 1 / direction.Y, 1 / direction.Z);
            return new Ray(start, end, inverse);
        }

        public static Ray FromScreen(ScreenPosition position, ScreenSize screenSize, Matrix4x4 invProjection)
        {
            return Ray.FromClip(Project.Clip(position, screenSize), invProjection);
        }

        public static Ray FromClip(Vector2 position, Matrix4x4 invProjection)
        {
            return Ray.FromPoints(Project.World(new Vector3(position, 0f), invProjection),
                Project.World(new Vector3(position, 1f), invProjection));
        }

        public Vector3 AtY0()
        {
            var direction = Vector3.Normalize(Start - End);
            direction = direction / direction.Y;
            return Start - (direction * Start.Y);
        }

        public Vector3 AtDistance(float distance)
        {
            var direction = Vector3.Normalize(Start - End);
            return Start + (direction * distance);
        }
    }
}
