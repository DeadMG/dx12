namespace Wrapper.Direct2D
{
    public interface IBrush : IDisposable
    {
        internal SharpDX.Direct2D1.Brush Native { get; }
    }
}
