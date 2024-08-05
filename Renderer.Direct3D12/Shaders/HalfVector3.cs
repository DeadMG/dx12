using System.Numerics;
using System.Runtime.InteropServices;

namespace Renderer.Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Explicit, Size = 6)]
    internal struct HalfVector3
    {
        [FieldOffset(0)]
        public Half X;

        [FieldOffset(2)]
        public Half Y;

        [FieldOffset(4)]
        public Half Z;

        public static implicit operator HalfVector3(Vector3 vec)
        {
            return new HalfVector3 { X = (Half)vec.X, Y = (Half)vec.Y, Z = (Half)vec.Z };
        }
    }
}
