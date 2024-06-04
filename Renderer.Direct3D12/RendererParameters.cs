using Data.Space;
using SharpDX.Direct3D12;
using Simulation;
using Util;

namespace Renderer
{
    public class RendererParameters
    {
        public required DisposeTracker Tracker { get; init; }
        public required ScreenSize ScreenSize { get; init; }

        public required CpuDescriptorHandle DepthBuffer { get; init; }
        public required CpuDescriptorHandle RenderTargetView { get; init; }

        public required Camera Camera { get; init; }
        public required Volume Volume { get; init; }
    }
}
