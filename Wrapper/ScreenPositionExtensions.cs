using Data;

namespace Wrapper
{
    public static class ScreenPositionExtensions
    {
        public static SharpDX.Mathematics.Interop.RawVector2 AsRawVector2(this ScreenPosition pos)
        {
            return new SharpDX.Mathematics.Interop.RawVector2 { X = pos.X, Y = pos.Y };
        }
    }
}
