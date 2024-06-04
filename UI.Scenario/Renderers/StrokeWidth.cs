using Simulation;

namespace UI.Renderers
{
    internal class StrokeWidth
    {
        public static float Scale(float stroke, Camera camera)
        {
            return (float)Math.Ceiling(stroke * 30 / camera.Position.Y);
        }
    }
}
