using Data.Space;
using Platform.Contracts;
using Simulation;
using System.Numerics;

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

            //foreach (var vertex in unit.Blueprint.Mesh.Vertices)
            //{
            //    var start = Project.Screen(Vector3.Transform(vertex.Position, unit.WorldMatrix), camera.ViewProjection, camera.ScreenSize);
            //    var end = Project.Screen(Vector3.Transform(vertex.Position + vertex.Normal, unit.WorldMatrix), camera.ViewProjection, camera.ScreenSize);
            //
            //    draw.DrawLine(new ScreenLine { Start = start, End = end }, brush);
            //}

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
