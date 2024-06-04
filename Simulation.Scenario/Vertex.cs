using Data.Space;
using System.Numerics;

namespace Simulation
{
    public struct Vertex
    {
        public required Vector3 Position { get; init; }
        public required RGB Colour { get; init; }
    }
}
