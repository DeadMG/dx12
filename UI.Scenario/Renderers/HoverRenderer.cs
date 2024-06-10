using Data.Space;
using Platform.Contracts;
using Simulation;

namespace UI.Renderers
{
    public class HoverRenderer : IDisposable
    {
        private readonly UnitHighlightRenderer highlightRenderer = new UnitHighlightRenderer();

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

            highlightRenderer.Render(hover, camera, brush, draw);
        }

        public void Dispose()
        {
            brush?.Dispose();
        }
    }
}
