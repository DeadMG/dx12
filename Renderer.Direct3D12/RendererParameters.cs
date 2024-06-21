using Data.Space;

namespace Renderer
{
    internal class RendererParameters
    {
        public required Vortice.Direct3D12.ID3D12Resource RenderTarget { get; init; }
    }
}
