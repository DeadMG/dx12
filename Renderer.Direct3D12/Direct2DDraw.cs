using Data.Space;
using Platform.Contracts;

namespace Renderer.Direct3D12
{
    public class Direct2DDraw : IDraw
    {
        private readonly Vortice.Direct2D1.ID2D1DeviceContext deviceContext;
        private readonly Vortice.Direct2D1.ID2D1Factory1 factory1;
        private ScreenSize screenSize;

        public Direct2DDraw(Vortice.Direct2D1.ID2D1Factory1 factory1, Vortice.Direct2D1.ID2D1DeviceContext deviceContext, ScreenSize screenSize)
        {
            this.deviceContext = deviceContext;
            this.screenSize = screenSize;
            this.factory1 = factory1;
        }

        public IBrush GetOrCreateSolidBrush(IBrush? existing, RGBA colour)
        {
            if (existing is SolidBrushWrapper wrapper && wrapper.Colour == colour) return wrapper;

            existing?.Dispose();
            return new SolidBrushWrapper { Colour = colour, Brush = deviceContext.CreateSolidColorBrush(colour.AsColour4()) };
        }

        public void DrawLine(ScreenLine line, IBrush brush, float strokeWidth = 1)
        {
            if (!(brush is NativeBrushWrapper wrapper)) throw new InvalidOperationException();

            deviceContext.DrawLine(line.Start.AsVector(), line.End.AsVector(), wrapper.Brush, strokeWidth);
        }

        public void FillRect(ScreenRectangle rect, IBrush brush)
        {
            if (!(brush is NativeBrushWrapper wrapper)) throw new InvalidOperationException();

            deviceContext.FillRectangle(rect.Clamp(screenSize).AsRawRectangleF(), wrapper.Brush);
        }

        public void Resize(ScreenSize screenSize)
        {
            this.screenSize = screenSize;
        }

        public void FillGeometry(ScreenPosition[] vertices, IBrush brush)
        {
            if (!(brush is NativeBrushWrapper wrapper)) throw new InvalidOperationException();

            using (var geometry = factory1.CreatePathGeometry())
            {
                using (var sink = geometry.Open())
                {
                    sink.SetFillMode(Vortice.Direct2D1.FillMode.Winding);
                    sink.BeginFigure(vertices[0].Clamp(screenSize).AsVector(), Vortice.Direct2D1.FigureBegin.Filled);
                    sink.AddLines(vertices.Skip(1).Select(s => s.Clamp(screenSize).AsVector()).ToArray());
                    sink.EndFigure(Vortice.Direct2D1.FigureEnd.Closed);
                    sink.Close();
                }

                deviceContext.FillGeometry(geometry, wrapper.Brush);
            }
        }

        private class NativeBrushWrapper : IBrush
        {
            public required Vortice.Direct2D1.ID2D1Brush Brush { get; init; }

            public void Dispose()
            {
                Brush.Dispose();
            }
        }

        private class SolidBrushWrapper : NativeBrushWrapper
        {
            public required RGBA Colour { get; init; }
        }
    }
}
