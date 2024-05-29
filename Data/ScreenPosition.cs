namespace Data
{
    public class ScreenPosition
    {
        public required int X { get; init; }
        public required int Y { get; init; }

        public ScreenPosition Min(ScreenPosition other)
        {
            return new ScreenPosition { X = Math.Min(X, other.X), Y = Math.Min(Y, other.Y) };
        }

        public ScreenPosition Max(ScreenPosition other)
        {
            return new ScreenPosition { X = Math.Max(X, other.X), Y = Math.Max(Y, other.Y) };
        }
    }
}
