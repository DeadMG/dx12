using System.Numerics;

namespace Data
{
    public struct Vertex
    {
        public required Vector3 Position { get; init; }
        public required Colour Colour { get; init; }
    }
}
