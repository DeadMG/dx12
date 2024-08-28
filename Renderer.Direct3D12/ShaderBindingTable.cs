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

        public void AddRayGeneration(string entry, Func<Vortice.Direct3D12.ID3D12Resource, byte[]> data) => rayGeneration.Add(new SBTData { Data = data, EntryPoint = entry });

        public int AddHit(string entry, Func<Vortice.Direct3D12.ID3D12Resource, byte[]> data)
        {
            hit.Add(new SBTData { Data = data, EntryPoint = entry });
            return hit.Count - 1;
        }

        public void AddMiss(string entry, Func<Vortice.Direct3D12.ID3D12Resource, byte[]> data) => miss.Add(new SBTData { Data = data, EntryPoint = entry });

        public Vortice.Direct3D12.DispatchRaysDescription Create(Vortice.Direct3D12.ID3D12Device5 device, Vortice.Direct3D12.ID3D12Resource tlas, PooledCommandList list)
        {
            var rayEntrySize = GetEntrySize(rayGeneration, tlas);
            var raySize = GetTableSize(rayGeneration, tlas);
            var hitEntrySize = GetEntrySize(hit, tlas);
            var hitSize = GetTableSize(hit, tlas);
            var missEntrySize = GetEntrySize(miss, tlas);
            var missSize = GetTableSize(miss, tlas);

            var totalSize = raySize + hitSize + missSize;

            var result = tracker.Track(device.CreateStaticBuffer(totalSize, Vortice.Direct3D12.ResourceStates.CopyDest).Name("SBT"));

            var data = new byte[totalSize];
            uint offset = 0;
            
            foreach (var rayEntry in rayGeneration)
            {
                var identifier = props.Get(rayEntry.EntryPoint);
                Array.Copy(identifier, 0, data, offset, identifier.Length);
                var rayData = rayEntry.Data(tlas);
                Array.Copy(rayData, 0, data, offset + identifier.Length, rayData.Length);
                offset += rayEntrySize;
            }

            offset = raySize;

            foreach (var hitEntry in hit)
            {
                var identifier = props.Get(hitEntry.EntryPoint);
                Array.Copy(identifier, 0, data, offset, identifier.Length);
                var hitData = hitEntry.Data(tlas);
                Array.Copy(hitData, 0, data, offset + identifier.Length, hitData.Length);
                offset += hitEntrySize;
            }

            offset = raySize + hitSize;

            foreach (var missEntry in miss)
            {
                var identifier = props.Get(missEntry.EntryPoint);
                Array.Copy(identifier, 0, data, offset, identifier.Length);
                var missData = missEntry.Data(tlas);
                Array.Copy(missData, 0, data, offset + identifier.Length, missData.Length);
                offset += missEntrySize;
            }

            var upload = list.CreateUploadBuffer(data).Name("SBT upload");
            tracker.Track(upload.Buffer);
            list.List.CopyResource(result, upload.Buffer);
            list.List.ResourceBarrierTransition(result, Vortice.Direct3D12.ResourceStates.CopyDest, Vortice.Direct3D12.ResourceStates.NonPixelShaderResource);

            return new Vortice.Direct3D12.DispatchRaysDescription
            {
                RayGenerationShaderRecord = new Vortice.Direct3D12.GpuVirtualAddressRange(result.GPUVirtualAddress, raySize),
                HitGroupTable = new Vortice.Direct3D12.GpuVirtualAddressRangeAndStride(result.GPUVirtualAddress + raySize, hitSize, hitEntrySize),
                MissShaderTable = new Vortice.Direct3D12.GpuVirtualAddressRangeAndStride(result.GPUVirtualAddress + raySize + hitSize, missSize, missEntrySize)
            };
        }

        private uint GetTableSize(List<SBTData> records, Vortice.Direct3D12.ID3D12Resource tlas)
        {
            return ((uint)records.Count * GetEntrySize(records, tlas)).Align(D3D12_RAYTRACING_SHADER_TABLE_BYTE_ALIGNMENT);
        }

        private uint GetEntrySize(List<SBTData> records, Vortice.Direct3D12.ID3D12Resource tlas)
        {
            return (StateObjectProperties.IdentifierSize + records.Max(x => (uint)x.Data(tlas).Length)).Align(D3D12_RAYTRACING_SHADER_TABLE_BYTE_ALIGNMENT);
        }

        public void Dispose()
        {
            tracker.Dispose();
        }

        private const int D3D12_RAYTRACING_SHADER_RECORD_BYTE_ALIGNMENT = 32;
        private const int D3D12_RAYTRACING_SHADER_TABLE_BYTE_ALIGNMENT = 64;

        private readonly record struct SBTData(string EntryPoint, Func<Vortice.Direct3D12.ID3D12Resource, byte[]> Data) { }
    }
}
