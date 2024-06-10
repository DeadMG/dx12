using System.Numerics;

namespace Util
{
    public static class Vector3Extensions
    {
        public static Vector2 DropZ(this Vector3 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }
    }
}
