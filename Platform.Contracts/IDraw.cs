using Data.Space;

namespace Platform.Contracts
{
    public interface IDraw
    {
        public IBrush GetOrCreateSolidBrush(IBrush? existing, RGBA colour);

        public void DrawLine(ScreenPosition start, ScreenPosition end, IBrush brush, float strokeWidth = 1);
        public void FillRect(ScreenRectangle rect, IBrush brush);
    }
}
