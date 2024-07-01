
namespace Renderer.Direct3D12.Shaders
{
    internal interface ILibrary : IDisposable
    {
        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects();
        public string Export { get; }
    }
}
