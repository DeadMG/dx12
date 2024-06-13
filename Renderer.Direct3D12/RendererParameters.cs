using Data.Space;
using Vortice.Direct3D12;
using Util;

namespace Renderer
{
    public class RendererParameters
    {
        public required DisposeTracker Tracker { get; init; }
        public required ScreenSize ScreenSize { get; init; }

        public required CpuDescriptorHandle DepthBuffer { get; init; }
        public required CpuDescriptorHandle RenderTargetView { get; init; }
    }
}
