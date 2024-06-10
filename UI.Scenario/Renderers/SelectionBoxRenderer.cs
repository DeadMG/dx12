using Data.Space;
using Platform.Contracts;

namespace UI.Renderers
{
    public class SelectionBoxRenderer : IDisposable
    {
        private IBrush? lineBrush;
        private IBrush? boxBrush;

        public RGBA SelectionBoxOutlineColour = new RGBA { B = 1, G = 1, R = 1, A = 1 };
        public RGBA SelectionBoxFillColour = new RGBA { B = 1, G = 1, R = 1, A = 0.5f };

        public void RenderSelectionBox(ScreenRectangle box, IDraw draw)
        {
            lineBrush = draw.GetOrCreateSolidBrush(lineBrush, SelectionBoxOutlineColour);
            boxBrush = draw.GetOrCreateSolidBrush(boxBrush, SelectionBoxFillColour);

            var topRight = new ScreenPosition { X = box.End.X, Y = box.Start.Y };
            var bottomLeft = new ScreenPosition { X = box.Start.X, Y = box.End.Y };

            draw.DrawLine(new ScreenLine(box.Start, topRight), lineBrush, 3);
            draw.DrawLine(new ScreenLine(box.Start, bottomLeft), lineBrush, 3);
            draw.DrawLine(new ScreenLine(box.End, bottomLeft), lineBrush, 3);
            draw.DrawLine(new ScreenLine(box.End, topRight), lineBrush, 3);
            draw.FillRect(box, boxBrush);
        }

        public void Dispose()
        {
            lineBrush?.Dispose();
            boxBrush?.Dispose();
        }
    }
}
