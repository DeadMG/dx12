using Data.Space;

namespace Platform.Contracts
{
    public interface IWindow
    {
        public IWindowListener? Listener { get; set; }

        public Task Closed { get; }
        public Task<ScreenSize> GetSize();
    }
}
