using Data.Space;
using Platform.Contracts;
using Simulation;

namespace UI.Renderers
{
    public class HighlightRenderer : IDisposable
    {
        private IBrush? brush;

        public RGBA HighlightLineColour = new RGBA { B = 1, G = 1, R = 1, A = 1 };

        public void RenderHighlight(Camera camera, HashSet<Unit> highlight, IDraw draw)
        {
            if (highlight.Count == 0) return;

            brush = draw.GetOrCreateSolidBrush(brush, HighlightLineColour);

            foreach (var unit in highlight)
            {
                var matrix = unit.WorldMatrix * camera.ViewProjection;
                var aabb = ScreenRectangle.FromPoints(unit.Blueprint.Mesh.Vertices.Select(v => Project.Screen(v.Position, matrix, camera.ScreenSize)));

                var width = Math.Abs(aabb.End.X - aabb.Start.X) / 10;
                var height = Math.Abs(aabb.End.Y - aabb.Start.Y) / 10;

                draw.DrawLine(aabb.Start, new ScreenPosition { X = aabb.Start.X + width, Y = aabb.Start.Y }, brush, StrokeWidth.Scale(3, camera));
                draw.DrawLine(aabb.Start, new ScreenPosition { X = aabb.Start.X, Y = aabb.Start.Y + height }, brush, StrokeWidth.Scale(3, camera));
                draw.DrawLine(aabb.End, new ScreenPosition { X = aabb.End.X - width, Y = aabb.End.Y }, brush, StrokeWidth.Scale(3, camera));
                draw.DrawLine(aabb.End, new ScreenPosition { X = aabb.End.X, Y = aabb.End.Y - height }, brush, StrokeWidth.Scale(3, camera));
            }
        }

        public void Dispose()
        {
            brush?.Dispose();
        }
    }
}
