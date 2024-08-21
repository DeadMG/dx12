namespace Renderer.Direct3D12.Shaders
{
    internal class RaytraceCommit
    {
        public required Vortice.Direct3D12.ID3D12Resource RenderTarget { get; init; }
        public required PooledCommandList List { get; init; }
        public required DescriptorHeapAccumulator HeapAccumulator { get; init; }
        public required ScreenSizeRaytraceResources ScreenSizeRaytraceResources { get; init; }
    }
}
