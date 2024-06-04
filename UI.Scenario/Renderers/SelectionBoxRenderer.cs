using Data.Space;
using Platform.Contracts;
using Simulation;

namespace UI.Renderers
{
    public class SelectionBoxRenderer : IDisposable
    {
        private IBrush? lineBrush;
        private IBrush? boxBrush;

        public RGBA SelectionBoxOutlineColour = new RGBA { B = 1, G = 1, R = 1, A = 1 };
        public RGBA SelectionBoxFillColour = new RGBA { B = 1, G = 1, R = 1, A = 0.5f };

        public void RenderSelectionBox(Camera camera, ScreenRectangle box, IDraw draw)
        {
            lineBrush = draw.GetOrCreateSolidBrush(lineBrush, SelectionBoxOutlineColour);
            boxBrush = draw.GetOrCreateSolidBrush(boxBrush, SelectionBoxFillColour);

            var topRight = new ScreenPosition { X = box.End.X, Y = box.Start.Y };
            var bottomLeft = new ScreenPosition { X = box.Start.X, Y = box.End.Y };

            draw.DrawLine(box.Start, topRight, lineBrush, StrokeWidth.Scale(3, camera));
            draw.DrawLine(box.Start, bottomLeft, lineBrush, StrokeWidth.Scale(3, camera));
            draw.DrawLine(box.End, bottomLeft, lineBrush, StrokeWidth.Scale(3, camera));
            draw.DrawLine(box.End, topRight, lineBrush, StrokeWidth.Scale(3, camera));
            draw.FillRect(box, boxBrush);
        }

        public void Dispose()
        {
            lineBrush?.Dispose();
            boxBrush?.Dispose();
        }
    }
}
