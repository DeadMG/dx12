using Data.Space;

namespace Renderer.Direct3D12.Shaders
{
    internal class RaytraceCommit
    {
        public required Vortice.Direct3D12.ID3D12Resource RenderTarget { get; init; }
        public required PooledCommandList List { get; init; }
        public required DescriptorHeapAccumulator HeapAccumulator { get; init; }
        public required ScreenSize ScreenSize { get; init; }
        public required Vortice.Direct3D12.ID3D12Resource RayGenSrv { get; init; }
        public required Vortice.Direct3D12.ID3D12Resource FilterSrv { get; init; }
        public required List<FrameData> Frames { get; init; }
    }
}
