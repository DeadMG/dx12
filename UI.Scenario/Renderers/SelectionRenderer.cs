using Data.Space;
using Simulation;
using Platform.Contracts;

namespace UI.Renderers
{
    public class SelectionRenderer : IDisposable
    {
        private IBrush? brush;

        public RGBA SelectionLineColour = new RGBA { B = 1, G = 1, R = 1, A = 1 };

        public void RenderSelection(Camera camera, HashSet<Unit> selection, IDraw draw)
        {
            if (selection.Count == 0) return;

            brush = draw.GetOrCreateSolidBrush(brush, SelectionLineColour);

            foreach (var unit in selection)
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
