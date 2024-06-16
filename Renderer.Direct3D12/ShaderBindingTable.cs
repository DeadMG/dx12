
using Util;

namespace Renderer.Direct3D12
{
    internal class ShaderBindingTable : IDisposable
    {
        private readonly List<SBTData> rayGeneration = new List<SBTData>();
        private readonly List<SBTData> hit = new List<SBTData>();
        private readonly List<SBTData> miss = new List<SBTData>();
        private readonly DisposeTracker tracker = new DisposeTracker();

        private readonly StateObjectProperties props;

        public ShaderBindingTable(StateObjectProperties props)
        {
            this.props = props;
        }

        public void AddRayGeneration(string entry, byte[] data) => rayGeneration.Add(new SBTData { Data = data, EntryPoint = entry });
        public void AddHit(string entry, byte[] data) => hit.Add(new SBTData { Data = data, EntryPoint = entry });
        public void AddMiss(string entry, byte[] data) => miss.Add(new SBTData { Data = data, EntryPoint = entry });

        public Vortice.Direct3D12.DispatchRaysDescription Create(Vortice.Direct3D12.ID3D12Device5 device, PooledCommandList list)
        {
            var rayEntrySize = GetEntrySize(rayGeneration);
            var raySize = GetTableSize(rayGeneration);
            var hitEntrySize = GetEntrySize(hit);
            var hitSize = GetTableSize(hit);
            var missEntrySize = GetEntrySize(miss);
            var missSize = GetTableSize(miss);

            var totalSize = raySize + hitSize + missSize;

            var result = tracker.Track(device.CreateStaticBuffer(totalSize, Vortice.Direct3D12.ResourceStates.CopyDest).Name("SBT"));

            var data = new byte[totalSize];
            uint offset = 0;
            
            foreach (var rayEntry in rayGeneration)
            {
                var identifier = props.Get(rayEntry.EntryPoint);
                Array.Copy(identifier, 0, data, offset, identifier.Length);
                Array.Copy(rayEntry.Data, 0, data, offset + identifier.Length, rayEntry.Data.Length);
                offset += rayEntrySize;
            }

            offset = raySize;

            foreach (var hitEntry in hit)
            {
                var identifier = props.Get(hitEntry.EntryPoint);
                Array.Copy(identifier, 0, data, offset, identifier.Length);
                Array.Copy(hitEntry.Data, 0, data, offset + identifier.Length, hitEntry.Data.Length);
                offset += hitEntrySize;
            }

            offset = raySize + hitSize;

            foreach (var missEntry in miss)
            {
                var identifier = props.Get(missEntry.EntryPoint);
                Array.Copy(identifier, 0, data, offset, identifier.Length);
                Array.Copy(missEntry.Data, 0, data, offset + identifier.Length, missEntry.Data.Length);
                offset += missEntrySize;
            }

            var upload = tracker.Track(list.CreateUploadBuffer(data).Name("SBT upload"));
            list.List.CopyResource(result, upload);
            list.List.ResourceBarrierTransition(result, Vortice.Direct3D12.ResourceStates.CopyDest, Vortice.Direct3D12.ResourceStates.NonPixelShaderResource);

            return new Vortice.Direct3D12.DispatchRaysDescription
            {
                RayGenerationShaderRecord = new Vortice.Direct3D12.GpuVirtualAddressRange(result.GPUVirtualAddress, raySize),
                HitGroupTable = new Vortice.Direct3D12.GpuVirtualAddressRangeAndStride(result.GPUVirtualAddress + raySize, hitSize, hitEntrySize),
                MissShaderTable = new Vortice.Direct3D12.GpuVirtualAddressRangeAndStride(result.GPUVirtualAddress + raySize + hitSize, missSize, missEntrySize)
            };
        }

        private uint GetTableSize(List<SBTData> records)
        {
            return ((uint)records.Count * GetEntrySize(records)).Align(D3D12_RAYTRACING_SHADER_TABLE_BYTE_ALIGNMENT);
        }

        private uint GetEntrySize(List<SBTData> records)
        {
            return StateObjectProperties.IdentifierSize + records.Max(x => (uint)x.Data.Length).Align(D3D12_RAYTRACING_SHADER_RECORD_BYTE_ALIGNMENT);
        }

        public void Dispose()
        {
            tracker.Dispose();
        }

        private const int D3D12_RAYTRACING_SHADER_RECORD_BYTE_ALIGNMENT = 32;
        private const int D3D12_RAYTRACING_SHADER_TABLE_BYTE_ALIGNMENT = 64;

        private readonly record struct SBTData(string EntryPoint, byte[] Data) { }
    }
}
