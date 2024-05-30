using Data;
using Simulation;
using System.Numerics;
using Wrapper;
using Wrapper.Direct3D;

namespace Renderer
{
    internal class RendererParameters
    {
        public required DisposeTracker Tracker { get; init; }
        public required DirectCommandList CommandList { get; init; }
        public required Device Device { get; init; }
        public required ResourceCache ResourceCache { get; init; }
        public required Matrix4x4 VPMatrix { get; init; }
        public required ScreenSize ScreenSize { get; init; }
        public required DepthBuffer DepthBuffer { get; init; }
        public required RenderTargetView RenderTargetView { get; init; }
        public required Pipeline Pipeline { get; init; }

        public required Player Player { get; init; }
        public required World World { get; init; }
    }
}
