using System.Numerics;

namespace Data
{
    public static class Vector4Extensions
    {
        public static Vector3 PerspectiveDivide(this Vector4 value)
        {
            // Don't allow annoying infinities
            return new Vector3(value.X, value.Y, value.Z) / Math.Max(value.W, 0.00001f);
        }
    }
}
