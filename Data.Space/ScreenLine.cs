namespace Data.Space
{
    // Does not need to be normalised; start and end may be in any order
    public readonly record struct ScreenLine(ScreenPosition Start, ScreenPosition End)
    {
    }
}
