using System.Numerics;

namespace Simulation.Physics
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

        public static Vector3 Abs(this Vector3 source)
        {
            return new Vector3(Math.Abs(source.X), Math.Abs(source.Y), Math.Abs(source.Z));
        }
    }
}
