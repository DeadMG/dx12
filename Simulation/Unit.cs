using Data;
using System.Numerics;

namespace Simulation
{
    public class Unit
    {
        public required Vector3 Position { get; set; }
        public required Quaternion Orientation { get; set; }
        public required Player Player { get; set; }

        public required Blueprint Blueprint { get; init; }
    }
}
