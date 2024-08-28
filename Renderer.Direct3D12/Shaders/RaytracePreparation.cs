using Simulation;

namespace Renderer.Direct3D12.Shaders
{
    internal class RaytracePreparation
    {
        public required ShaderBindingTable ShaderTable { get; init; }
        public required Volume Volume { get; init; }
        public required Camera Camera { get; init; }
        public required PooledCommandList List { get; init; }
        public required List<Vortice.Direct3D12.RaytracingInstanceDescription> InstanceDescriptions { get; init; }
        public required DescriptorHeapAccumulator HeapAccumulator { get; init; }
        public required Vortice.Direct3D12.ID3D12Resource RayGenSrv { get; init; }
        public required Vortice.Direct3D12.ID3D12Resource FilterSrv { get; init; }
        public required StructuredBuffer Data { get; init; }
    }
}
