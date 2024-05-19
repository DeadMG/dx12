using System.Numerics;

namespace Simulation
{
    public class World
    {
        public List<Unit> Units { get; set; } = new List<Unit>();
        public Vector3 CameraPosition { get; set; }
        public Quaternion CameraOrientation { get; set; }
    }
}
