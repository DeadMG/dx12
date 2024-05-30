namespace Data
{
    public class Blueprint
    {
        public required string Name { get; init; }
        public required Mesh Mesh { get; init; }

        public required float TurnRate { get; init; } // Radians per second
        public required float Acceleration { get; init; }
        public required float MaxSpeed { get; init; }
    }
}
