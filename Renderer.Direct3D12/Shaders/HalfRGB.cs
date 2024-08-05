using Data.Space;
using System.Runtime.InteropServices;

namespace Renderer.Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Explicit, Size = 6)]
    internal struct HalfRGB
    {
        [FieldOffset(0)]
        public Half R;

        [FieldOffset(2)]
        public Half G;

        [FieldOffset(4)]
        public Half B;

        public static implicit operator HalfRGB(RGB rgb)
        {
            return new HalfRGB { R = (Half)rgb.R, G = (Half)rgb.G, B = (Half)rgb.B };
        }
    }
}
