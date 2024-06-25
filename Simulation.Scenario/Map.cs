using Data.Mesh;
using System.Numerics;

namespace Simulation
{
    public class Map
    {
        public required float AmbientLightLevel { get; init; }
        public required Vector3 Dimensions { get; init; }
        public required PredefinedObject[] Objects { get; init; }
    }

    public struct PredefinedObject
    {
        public required Vector3 Position { get; init; }
        public required float Size { get; init; }
        public required Mesh Mesh { get; init; }
        public required string Name { get; init; }
    }
}
