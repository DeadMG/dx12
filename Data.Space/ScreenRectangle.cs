namespace Data.Space
{
    // Expected to be normalised, i.e. start is top left, end is bottom right
    public readonly record struct ScreenRectangle(ScreenPosition Start, ScreenPosition End)
    {
        public static ScreenRectangle FromPoints(ScreenPosition start, ScreenPosition end)
        {
            return new ScreenRectangle { Start = start.Min(end), End = start.Max(end) };
        }

        public ScreenRectangle Clamp(ScreenSize size)
        {
            return new ScreenRectangle(Start.Clamp(size), End.Clamp(size));
        }

        public static ScreenRectangle FromPoints(IEnumerable<ScreenPosition> points)
        {
            ScreenPosition? start = null;
            ScreenPosition? end = null;

            foreach (var point in points)
            {
                if (start == null)
                {
                    start = point;
                    end = point;
                    continue;
                }

                start = start.Value.Min(point);
                end = end.Value.Max(point);
            }

            return ScreenRectangle.FromPoints(start.Value, end.Value);
        }
    }
}
