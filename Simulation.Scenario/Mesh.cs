using System.Numerics;

namespace Simulation
{
    public class Mesh
    {
        public required Vertex[] Vertices { get; init; }
        public required short[] Indices { get; init; }

        public static readonly Vector3 Facing = new Vector3(0, 0, 1);
    }
}
