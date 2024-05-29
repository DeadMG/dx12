using Data;

namespace Wrapper.Direct2D
{
    public class DrawContext
    {
        private readonly SharpDX.Direct2D1.DeviceContext deviceContext;

        internal DrawContext(SharpDX.Direct2D1.DeviceContext deviceContext)
        {
            this.deviceContext = deviceContext;
        }

        public void DrawLine(ScreenPosition start, ScreenPosition end, IBrush brush)
        {
            deviceContext.DrawLine(start.AsRawVector2(), end.AsRawVector2(), brush.Native);
        }

        public void FillRect(ScreenRectangle rect, IBrush brush)
        {
            deviceContext.FillRectangle(rect.AsRawRectangleF(), brush.Native);
        }

        public IBrush CreateSolidBrush(Colour colour)
        {
            return new SolidColourBrush { Native = new SharpDX.Direct2D1.SolidColorBrush(deviceContext, colour.AsColour4()) };
        }

        private class SolidColourBrush : IBrush
        {
            public required SharpDX.Direct2D1.Brush Native { get; init; }

            public void Dispose()
            {
                Native.Dispose();
            }
        }
    }
}
