using Data.Space;
using Platform.Contracts;

namespace Renderer.Direct3D12
{
    public class Direct2DDraw : IDraw
    {
        private readonly SharpDX.Direct2D1.DeviceContext deviceContext;

        public Direct2DDraw(SharpDX.Direct2D1.DeviceContext deviceContext)
        {
            this.deviceContext = deviceContext;
        }

        public IBrush GetOrCreateSolidBrush(IBrush? existing, RGBA colour)
        {
            if (existing is SolidBrushWrapper wrapper && wrapper.Colour == colour) return wrapper;

            existing?.Dispose();
            return new SolidBrushWrapper { Colour = colour, Brush = new SharpDX.Direct2D1.SolidColorBrush(deviceContext, colour.AsColour4()) };
        }

        public void DrawLine(ScreenPosition start, ScreenPosition end, IBrush brush, float strokeWidth = 1)
        {
            if (!(brush is NativeBrushWrapper wrapper)) throw new InvalidOperationException();

            deviceContext.DrawLine(start.AsRawVector2(), end.AsRawVector2(), wrapper.Brush, strokeWidth);
        }

        public void FillRect(ScreenRectangle rect, IBrush brush)
        {
            if (!(brush is NativeBrushWrapper wrapper)) throw new InvalidOperationException();

            deviceContext.FillRectangle(rect.AsRawRectangleF(), wrapper.Brush);
        }

        private class NativeBrushWrapper : IBrush
        {
            public required SharpDX.Direct2D1.Brush Brush { get; init; }

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
