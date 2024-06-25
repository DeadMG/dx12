using Data.Space;

namespace Data.Mesh
{
    public struct Material
    {
        public required RGB Colour { get; init; }
        public required RGB EmissionColour { get; init; }
        public required float EmissionStrength { get; init; }
    }
}
