using Data.Space;

namespace Renderer
{
    internal class RendererParameters
    {
        public required ScreenSize ScreenSize { get; init; }

        public required Vortice.Direct3D12.CpuDescriptorHandle DepthBuffer { get; init; }
        public required Vortice.Direct3D12.CpuDescriptorHandle RenderTargetDescriptor { get; init; }
        public required Vortice.Direct3D12.ID3D12Resource RenderTarget { get; init; }
    }
}
