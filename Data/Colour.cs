namespace Data
{
    public readonly struct Colour
    {
        public Colour()
        {
        }

        public required float R { get; init; }
        public required float G { get; init; }
        public required float B { get; init; }
        public float A { get; init; } = 1;
    }
}
