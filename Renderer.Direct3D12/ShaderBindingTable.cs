namespace Renderer.Direct3D12
{
    internal class ShaderBindingTable
    {
        private readonly List<SBTData> rayGeneration = new List<SBTData>();
        private readonly List<SBTData> hit = new List<SBTData>();
        private readonly List<SBTData> miss = new List<SBTData>();

        private readonly StateObjectProperties props;

        public ShaderBindingTable(StateObjectProperties props)
        {
            this.props = props;
        }

        public void AddRayGeneration(string entry, byte[] data) => rayGeneration.Add(new SBTData { Data = data, EntryPoint = entry });

        public int AddHit(string entry, byte[] data)
        {
            hit.Add(new SBTData { Data = data, EntryPoint = entry });
            return hit.Count - 1;
        }

        public void AddMiss(string entry, byte[] data) => miss.Add(new SBTData { Data = data, EntryPoint = entry });

        public Vortice.Direct3D12.DispatchRaysDescription Create(FrameResources frameResources)
        {
            var rayEntrySize = GetEntrySize(rayGeneration);
            var raySize = GetTableSize(rayGeneration);
            var hitEntrySize = GetEntrySize(hit);
            var hitSize = GetTableSize(hit);
            var missEntrySize = GetEntrySize(miss);
            var missSize = GetTableSize(miss);

            var totalSize = raySize + hitSize + missSize;

            var data = new byte[totalSize];
            uint offset = 0;
            
            foreach (var rayEntry in rayGeneration)
            {
                var identifier = props.Get(rayEntry.EntryPoint);
                Array.Copy(identifier, 0, data, offset, identifier.Length);
                var rayData = rayEntry.Data;
                Array.Copy(rayData, 0, data, offset + identifier.Length, rayData.Length);
                offset += rayEntrySize;
            }

            offset = raySize;

            foreach (var hitEntry in hit)
            {
                var identifier = props.Get(hitEntry.EntryPoint);
                Array.Copy(identifier, 0, data, offset, identifier.Length);
                var hitData = hitEntry.Data;
                Array.Copy(hitData, 0, data, offset + identifier.Length, hitData.Length);
                offset += hitEntrySize;
            }

            offset = raySize + hitSize;

            foreach (var missEntry in miss)
            {
                var identifier = props.Get(missEntry.EntryPoint);
                Array.Copy(identifier, 0, data, offset, identifier.Length);
                var missData = missEntry.Data;
                Array.Copy(missData, 0, data, offset + identifier.Length, missData.Length);
                offset += missEntrySize;
            }

            var result = frameResources.TransferToUnorderedAccess(data, D3D12_RAYTRACING_SHADER_TABLE_BYTE_ALIGNMENT);

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
            return (StateObjectProperties.IdentifierSize + records.Max(x => (uint)x.Data.Length)).Align(D3D12_RAYTRACING_SHADER_TABLE_BYTE_ALIGNMENT);
        }

        private const int D3D12_RAYTRACING_SHADER_RECORD_BYTE_ALIGNMENT = 32;
        private const int D3D12_RAYTRACING_SHADER_TABLE_BYTE_ALIGNMENT = 64;

        private readonly record struct SBTData(string EntryPoint, byte[] Data) { }
    }
}
