using Data.Mesh;
using Data.Space;
using System.Numerics;

namespace Simulation
{
    public class Map
    {
        public required float AmbientLightLevel { get; init; }
        public required Vector3 Dimensions { get; init; }
        public required PredefinedObject[] Objects { get; init; }

        public required float StarfieldNoiseScale { get; init; }
        public required float StarfieldNoiseCutoff { get; init; }
        public required float StarfieldTemperatureScale { get; init; }
        public required uint? StarfieldSeed { get; init; }

        public required StarCategory[] StarCategories { get; init; }

        public required string Name { get; init; }
        public required Guid Id { get; init; }
    }

    public struct PredefinedObject
    {
        public required Vector3 Position { get; init; }
        public required float Size { get; init; }
        public required Mesh Mesh { get; init; }
        public required string Name { get; init; }
    }

    public struct StarCategory
    {
        public required float Cutoff { get; init; }
        public required RGB Colour { get; init; }
    }
}
