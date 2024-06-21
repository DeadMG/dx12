using Data.Space;
using System.Numerics;

namespace Simulation
{
    public class Map
    {
        public required float AmbientLightLevel { get; init; }
        public required Vector3 Dimensions { get; init; }
        public required Sun[] Suns { get; init; }
    }

    public class Sun
    {
        public required RGB MeshColour { get; init; }

        public required RGB LightColour { get; init; }
        public required float LightIntensity { get; init; }

        public required Vector3 Position { get; init; }
        public required float Size { get; init; }
    }
}
