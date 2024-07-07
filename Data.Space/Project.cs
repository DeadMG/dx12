using System.Numerics;

namespace Data.Space
{
    public static class Project
    {
        public static ScreenPosition Screen(Vector2 clip, ScreenSize screenSize)
        {
            return new ScreenPosition(Screen(clip.X, screenSize.Width), Screen(-clip.Y, screenSize.Height));
        }

        public static ScreenPosition Screen(Vector3 clip, ScreenSize screenSize)
        {
            return Screen(new Vector2(clip.X, clip.Y), screenSize);
        }

        public static ScreenPosition Screen(Vector3 worldPosition, Matrix4x4 projectionMatrix, ScreenSize screenSize)
        {
            return Screen(Clip(worldPosition, projectionMatrix), screenSize);
        }

        public static Vector3 Clip(Vector3 worldPosition, Matrix4x4 projectionMatrix)
        {
            return PerspectiveDivide(Vector4.Transform(new Vector4(worldPosition, 1.0f), projectionMatrix));
        }

        public static Vector2 Clip(ScreenPosition position, ScreenSize screenSize)
        {
            return new Vector2(Clip(position.X, screenSize.Width), -Clip(position.Y, screenSize.Height));
        }

        public static Vector3 World(Vector3 clip, Matrix4x4 invProjectionMatrix)
        {
            return PerspectiveDivide(Vector4.Transform(new Vector4(clip, 1), invProjectionMatrix));
        }

        private static int Screen(float clip, int max)
        {
            return (int)Math.Floor(((clip / 2) + 0.5f) * max);
        }

        private static float Clip(int screen, int max)
        {
            return ((screen / (float)max) * 2) - 1;
        }

        public static Vector3 PerspectiveDivide(Vector4 value)
        {
            // Don't allow annoying infinities
            return new Vector3(value.X, value.Y, value.Z) / Math.Max(value.W, 0.00001f);
        }
    }
}
