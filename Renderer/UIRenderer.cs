using Data;
using Simulation;
using Wrapper.Direct2D;

namespace Renderer
{
    internal class UIRenderer
    {
        public Colour HoverLineColour = new Colour { B = 1, G = 1, R = 1 };
        public Colour HighlightLineColour = new Colour { B = 1, G = 1, R = 1 };
        public Colour SelectionLineColour = new Colour { B = 1, G = 1, R = 1 };
        public Colour SelectionBoxOutlineColour = new Colour { B = 1, G = 1, R = 1 };
        public Colour SelectionBoxFillColour = new Colour { B = 1, G = 1, R = 1, A = 0.5f };

        internal void Render(RendererParameters rp, Player player, DrawContext draw)
        {
            var hoverBrush = rp.Tracker.Track(draw.CreateSolidBrush(HoverLineColour));
            var highlightBrush = rp.Tracker.Track(draw.CreateSolidBrush(HighlightLineColour));
            var selectionBrush = rp.Tracker.Track(draw.CreateSolidBrush(SelectionLineColour));
            var selectionBoxOutlineBrush = rp.Tracker.Track(draw.CreateSolidBrush(SelectionBoxOutlineColour));
            var selectionBoxFillBrush = rp.Tracker.Track(draw.CreateSolidBrush(SelectionBoxFillColour));

            if (player.Hover != null)
            {
                var matrix = player.Hover.WorldMatrix * rp.VPMatrix;
                var aabb = AABB.FromVertices(player.Hover.Blueprint.Mesh.Vertices.Select(v => v.Position.PerspectiveTransform(matrix)));

                var screenStart = Space.Screen(aabb.Start.DropZ(), rp.ScreenSize);
                var screenEnd = Space.Screen(aabb.End.DropZ(), rp.ScreenSize);

                var width = Math.Abs(screenEnd.X - screenStart.X) / 10;
                var height = Math.Abs(screenEnd.Y - screenStart.Y) / 10;

                draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X + width, Y = screenStart.Y }, hoverBrush);
                draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X, Y = screenStart.Y - height }, hoverBrush);
                draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X - width, Y = screenEnd.Y }, hoverBrush);
                draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X, Y = screenEnd.Y + height }, hoverBrush);
            }

            foreach (var unit in player.Highlight)
            {
                var matrix = unit.WorldMatrix * rp.VPMatrix;
                var aabb = AABB.FromVertices(unit.Blueprint.Mesh.Vertices.Select(v => v.Position.PerspectiveTransform(matrix)));

                var screenStart = Space.Screen(aabb.Start.DropZ(), rp.ScreenSize);
                var screenEnd = Space.Screen(aabb.End.DropZ(), rp.ScreenSize);

                var width = Math.Abs(screenEnd.X - screenStart.X) / 10;
                var height = Math.Abs(screenEnd.Y - screenStart.Y) / 10;

                draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X + width, Y = screenStart.Y }, highlightBrush);
                draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X, Y = screenStart.Y - height }, highlightBrush);
                draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X - width, Y = screenEnd.Y }, highlightBrush);
                draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X, Y = screenEnd.Y + height }, highlightBrush);
            }

            foreach (var unit in player.Selection)
            {
                var matrix = unit.WorldMatrix * rp.VPMatrix;
                var aabb = AABB.FromVertices(unit.Blueprint.Mesh.Vertices.Select(v => v.Position.PerspectiveTransform(matrix)));

                var screenStart = Space.Screen(aabb.Start.DropZ(), rp.ScreenSize);
                var screenEnd = Space.Screen(aabb.End.DropZ(), rp.ScreenSize);

                var width = Math.Abs(screenEnd.X - screenStart.X) / 10;
                var height = Math.Abs(screenEnd.Y - screenStart.Y) / 10;

                draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X + width, Y = screenStart.Y }, selectionBrush);
                draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X, Y = screenStart.Y - height }, selectionBrush);
                draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X - width, Y = screenEnd.Y }, selectionBrush);
                draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X, Y = screenEnd.Y + height }, selectionBrush);
            }

            if (player.SelectionHighlight != null)
            {
                var bottomLeft = player.SelectionHighlight.Start.Min(player.SelectionHighlight.End);
                var topRight = player.SelectionHighlight.Start.Max(player.SelectionHighlight.End);
                var bottomRight = new ScreenPosition { X = topRight.X, Y = bottomLeft.Y };
                var topLeft = new ScreenPosition { X = bottomLeft.X, Y = topRight.Y };

                draw.DrawLine(bottomLeft, bottomRight, selectionBoxOutlineBrush);
                draw.DrawLine(bottomLeft, topLeft, selectionBoxOutlineBrush);
                draw.DrawLine(topRight, topLeft, selectionBoxOutlineBrush);
                draw.DrawLine(topRight, bottomRight, selectionBoxOutlineBrush);
                draw.FillRect(player.SelectionHighlight, selectionBoxFillBrush);
            }
        }
    }
}
