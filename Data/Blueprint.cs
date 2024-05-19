using System.Numerics;

namespace Data
{
    public class Blueprint
    {
        public required string Name { get; init; }
        public required Mesh Mesh { get; init; }
    }

    public struct Vertex
    {
        public required Vector3 Position { get; init; }
        public required Colour Colour { get; init; }
    }

    public struct Colour
    {
        public required float R { get; init; }
        public required float G { get; init; }
        public required float B { get; init; }
    }

    public class Mesh
    {
        public required Vertex[] Vertices { get; init; }
        public required short[] Indices { get; init; }
    }
}
