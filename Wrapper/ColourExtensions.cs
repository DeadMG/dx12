using Data;

namespace Wrapper
{
    public static class ColourExtensions
    {
        public static SharpDX.Mathematics.Interop.RawColor4 AsColour4(this Colour colour)
        {
            return new SharpDX.Mathematics.Interop.RawColor4(colour.R * colour.A, colour.G * colour.A, colour.B * colour.A, colour.A);
        }
    }
}
