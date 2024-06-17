using Data.Space;
using Platform.Contracts;
using Simulation;

namespace UI.Renderers
{
    public class UnitHighlightRenderer
    {
        public void Render(Unit unit, Camera camera, IBrush brush, IDraw draw)
        {
            var matrix = unit.WorldMatrix * camera.ViewProjection;
            var aabb = ScreenRectangle.FromPoints(unit.Blueprint.Mesh.Vertices.Select(v => Project.Screen(v.Position, matrix, camera.ScreenSize)));

            var width = Math.Abs(aabb.End.X - aabb.Start.X) / 3;
            var height = Math.Abs(aabb.End.Y - aabb.Start.Y) / 3;

            var thickness = Math.Max((width + height) / 30, 1);

            draw.FillGeometry(
                new ScreenPosition[]
                {
                        aabb.Start,
                        aabb.Start + new ScreenPosition(width, 0),
                        aabb.Start + new ScreenPosition(width, -thickness),
                        aabb.Start + new ScreenPosition(-thickness, -thickness),
                        aabb.Start + new ScreenPosition(-thickness, height),
                        aabb.Start + new ScreenPosition(0, height)
                },
                brush);

            draw.FillGeometry(
                new ScreenPosition[]
                {
                        aabb.End,
                        aabb.End - new ScreenPosition(width, 0),
                        aabb.End - new ScreenPosition(width, -thickness),
                        aabb.End - new ScreenPosition(-thickness, -thickness),
                        aabb.End - new ScreenPosition(-thickness, height),
                        aabb.End - new ScreenPosition(0, height)
                },
                brush);
        }
    }
}
