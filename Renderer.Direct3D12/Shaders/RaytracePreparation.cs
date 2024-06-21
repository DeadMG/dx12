using Data.Space;
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
        public required List<Vortice.Direct3D12.ID3D12DescriptorHeap> DescriptorHeaps { get; init; }
    }
}
