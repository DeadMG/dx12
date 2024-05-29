using System.Numerics;

namespace Data
{
    public static class Space
    {
        public static Vector2 Clip(ScreenPosition position, ScreenSize screenSize)
        {
            return new Vector2(Clip(position.X, screenSize.Width), -Clip(position.Y, screenSize.Height));
        }

        public static ScreenPosition Screen(Vector2 clip, ScreenSize screenSize)
        {
            return new ScreenPosition { X = Screen(clip.X, screenSize.Width), Y = Screen(-clip.Y, screenSize.Height) };
        }

        private static int Screen(float clip, int max)
        {
            return (int)Math.Floor(((clip / 2) + 0.5f) * max);
        }

        private static float Clip(int screen, int max)
        {
            return ((screen / (float)max) * 2) - 1;
        }
    }
}
