using Data.Space;
using Simulation;

namespace Platform.Contracts
{
    public interface IRenderer : IDisposable
    {
        void Resize(ScreenSize size);
        Task Render(Camera camera, Volume volume, Action<IDraw> uiRenderer);
    }
}
