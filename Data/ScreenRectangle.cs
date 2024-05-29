namespace Data
{
    public class ScreenRectangle
    {
        public required ScreenPosition Start { get; init; }
        public required ScreenPosition End { get; init; }

        public ScreenRectangle Normalize()
        {
            return new ScreenRectangle { Start = Start.Min(End), End = Start.Max(End) };
        }
    }
}
