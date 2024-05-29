using System.Numerics;

namespace Data
{
    public class Ray
    {
        private readonly Vector3 start;
        private readonly Vector3 end;
        private readonly Vector3 inverse;

        public Ray(Vector3 start, Vector3 end)
        {
            this.start = start;
            this.end = end;
            var direction = Vector3.Normalize(end - start);
            inverse = new Vector3(1/direction.X, 1/direction.Y, 1/direction.Z);
        }

        public Vector3 Start => start;
        public Vector3 End => end;
        public Vector3 Inverse => inverse;

        public Vector3 AtY0()
        {
            var direction = Vector3.Normalize(start - end);
            direction = direction / direction.Y;
            return start - (direction * start.Y);
        }
    }
}
