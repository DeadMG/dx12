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

        internal void Render(RendererParameters rp, DrawContext draw)
        {
            RenderHover(rp, rp.Player.Hover, draw);
            RenderHighlight(rp, rp.Player.Highlight, draw);
            RenderSelection(rp, rp.Player.Selection, draw);
            RenderSelectionBox(rp, rp.Player.SelectionBox, draw);
        }

        private void RenderHover(RendererParameters rp, Unit? hover, DrawContext draw)
        {
            if (hover == null) return;

            var hoverBrush = rp.Tracker.Track(draw.CreateSolidBrush(HoverLineColour));

            var matrix = hover.WorldMatrix * rp.VPMatrix;
            var aabb = AABB.FromVertices(hover.Blueprint.Mesh.Vertices.Select(v => v.Position.PerspectiveTransform(matrix)));

            var screenStart = Space.Screen(aabb.Start.DropZ(), rp.ScreenSize);
            var screenEnd = Space.Screen(aabb.End.DropZ(), rp.ScreenSize);

            var width = Math.Abs(screenEnd.X - screenStart.X) / 10;
            var height = Math.Abs(screenEnd.Y - screenStart.Y) / 10;

            draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X + width, Y = screenStart.Y }, hoverBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
            draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X, Y = screenStart.Y - height }, hoverBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
            draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X - width, Y = screenEnd.Y }, hoverBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
            draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X, Y = screenEnd.Y + height }, hoverBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
        }

        private void RenderHighlight(RendererParameters rp, HashSet<Unit> highlight, DrawContext draw)
        {
            if (highlight.Count == 0) return;

            var highlightBrush = rp.Tracker.Track(draw.CreateSolidBrush(HighlightLineColour));
            foreach (var unit in highlight)
            {
                var matrix = unit.WorldMatrix * rp.VPMatrix;
                var aabb = AABB.FromVertices(unit.Blueprint.Mesh.Vertices.Select(v => v.Position.PerspectiveTransform(matrix)));

                var screenStart = Space.Screen(aabb.Start.DropZ(), rp.ScreenSize);
                var screenEnd = Space.Screen(aabb.End.DropZ(), rp.ScreenSize);

                var width = Math.Abs(screenEnd.X - screenStart.X) / 10;
                var height = Math.Abs(screenEnd.Y - screenStart.Y) / 10;

                draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X + width, Y = screenStart.Y }, highlightBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
                draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X, Y = screenStart.Y - height }, highlightBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
                draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X - width, Y = screenEnd.Y }, highlightBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
                draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X, Y = screenEnd.Y + height }, highlightBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
            }
        }

        private void RenderSelection(RendererParameters rp, HashSet<Unit> selection, DrawContext draw)
        {
            if (selection.Count == 0) return;

            var selectionBrush = rp.Tracker.Track(draw.CreateSolidBrush(SelectionLineColour));
            foreach (var unit in selection)
            {
                var matrix = unit.WorldMatrix * rp.VPMatrix;
                var aabb = AABB.FromVertices(unit.Blueprint.Mesh.Vertices.Select(v => v.Position.PerspectiveTransform(matrix)));

                var screenStart = Space.Screen(aabb.Start.DropZ(), rp.ScreenSize);
                var screenEnd = Space.Screen(aabb.End.DropZ(), rp.ScreenSize);

                var width = Math.Abs(screenEnd.X - screenStart.X) / 10;
                var height = Math.Abs(screenEnd.Y - screenStart.Y) / 10;

                draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X + width, Y = screenStart.Y }, selectionBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
                draw.DrawLine(screenStart, new ScreenPosition { X = screenStart.X, Y = screenStart.Y - height }, selectionBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
                draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X - width, Y = screenEnd.Y }, selectionBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
                draw.DrawLine(screenEnd, new ScreenPosition { X = screenEnd.X, Y = screenEnd.Y + height }, selectionBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
            }
        }

        private void RenderSelectionBox(RendererParameters rp, ScreenRectangle? box, DrawContext draw)
        {
            if (box == null) return;

            var selectionBoxOutlineBrush = rp.Tracker.Track(draw.CreateSolidBrush(SelectionBoxOutlineColour));
            var selectionBoxFillBrush = rp.Tracker.Track(draw.CreateSolidBrush(SelectionBoxFillColour));

            var bottomLeft = box.Start.Min(box.End);
            var topRight = box.Start.Max(box.End);
            var bottomRight = new ScreenPosition { X = topRight.X, Y = bottomLeft.Y };
            var topLeft = new ScreenPosition { X = bottomLeft.X, Y = topRight.Y };

            draw.DrawLine(bottomLeft, bottomRight, selectionBoxOutlineBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
            draw.DrawLine(bottomLeft, topLeft, selectionBoxOutlineBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
            draw.DrawLine(topRight, topLeft, selectionBoxOutlineBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
            draw.DrawLine(topRight, bottomRight, selectionBoxOutlineBrush, ScaleStrokeWidth(3, rp.Player, rp.World));
            draw.FillRect(box, selectionBoxFillBrush);
        }

        private float ScaleStrokeWidth(float stroke, Player player, World world)
        {
            return (float)Math.Ceiling((stroke * 30) / player.CameraFor(world).Position.Y);
        }
    }
}
