using Data.Space;
using Platform.Contracts;
using Simulation;

namespace UI.Renderers
{
    public class HoverRenderer : IDisposable
    {
        private IBrush? brush;

        public RGBA HoverLineColour = new RGBA { B = 1, G = 1, R = 1, A = 1 };

        public void RenderHover(Camera camera, Unit? hover, IDraw draw)
        {
            if (hover == null) return;

            brush = draw.GetOrCreateSolidBrush(brush, HoverLineColour);

            var matrix = hover.WorldMatrix * camera.ViewProjection;
            var box = ScreenRectangle.FromPoints(hover.Blueprint.Mesh.Vertices.Select(v => Project.Screen(v.Position, matrix, camera.ScreenSize)));

            var width = Math.Abs(box.End.X - box.Start.X) / 10;
            var height = Math.Abs(box.End.Y - box.Start.Y) / 10;

            draw.DrawLine(box.Start, new ScreenPosition { X = box.Start.X + width, Y = box.Start.Y }, brush, StrokeWidth.Scale(3, camera));
            draw.DrawLine(box.Start, new ScreenPosition { X = box.Start.X, Y = box.Start.Y + height }, brush, StrokeWidth.Scale(3, camera));
            draw.DrawLine(box.End, new ScreenPosition { X = box.End.X - width, Y = box.End.Y }, brush, StrokeWidth.Scale(3, camera));
            draw.DrawLine(box.End, new ScreenPosition { X = box.End.X, Y = box.End.Y - height }, brush, StrokeWidth.Scale(3, camera));
        }

        public void Dispose()
        {
            brush?.Dispose();
        }
    }
}
