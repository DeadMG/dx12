using Data.Space;

namespace Platform.Contracts
{
    public interface IRenderer : IDisposable
    {
        void Resize(ScreenSize size);
        Task Render(VolumeRenderTask volumeRender, Action<IDraw> uiRender);
    }
}
