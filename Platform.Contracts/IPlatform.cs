namespace Platform.Contracts
{
    public interface IPlatform
    {
        public Task OneTimeInitialisation();
        public IWindow CreateWindow();
        public Task<IRenderer> CreateRenderer(IWindow window);
    }
}
