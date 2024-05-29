using System.Numerics;

namespace Data
{
    public static class Vector3Extensions
    {
        public static Vector3 Min(this Vector3 source, Vector3 other)
        {
            return new Vector3(Math.Min(source.X, other.X), 
                Math.Min(source.Y, other.Y),
                Math.Min(source.Z, other.Z));
        }

        public static Vector3 Max(this Vector3 source, Vector3 other)
        {
            return new Vector3(Math.Max(source.X, other.X),
                Math.Max(source.Y, other.Y), 
                Math.Max(source.Z, other.Z));
        }

        public static Vector3 PerspectiveTransform(this Vector3 source, Matrix4x4 matrix)
        {
            return Vector4.Transform(new Vector4(source, 1), matrix).PerspectiveDivide();
        }

        public static Vector2 DropZ(this Vector3 source)
        {
            return new Vector2(source.X, source.Y);
        }

        public static Vector3 Abs(this Vector3 source)
        {
            return new Vector3(Math.Abs(source.X), Math.Abs(source.Y), Math.Abs(source.Z));
        }
    }
}
