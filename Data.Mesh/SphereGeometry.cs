namespace Data.Mesh
{
    public class SphereGeometry : IGeometry
    {
        public required Material Material { get; init; }
        public required bool DistanceIndependentEmission { get; init; }
    }
}
