using Data;

namespace Wrapper
{
    public static class ScreenRectangleExtensions
    {
        public static SharpDX.Mathematics.Interop.RawRectangleF AsRawRectangleF(this ScreenRectangle rect)
        {
            var normal = rect.Normalize();
            return new SharpDX.Mathematics.Interop.RawRectangleF { Top = normal.Start.Y, Left = normal.Start.X, Right = normal.End.X, Bottom = normal.End.Y };
        }
    }
}
