namespace Renderer.Direct3D12
{
    internal static class D3D12Extensions
    {
        public static Vortice.Direct3D12.ID3D12Resource CreateStaticBuffer(this Vortice.Direct3D12.ID3D12Device5 device, uint size, Vortice.Direct3D12.ResourceStates initialState = Vortice.Direct3D12.ResourceStates.Common, Vortice.Direct3D12.ResourceFlags flags = Vortice.Direct3D12.ResourceFlags.None)
        {
            return device.CreateCommittedResource(new Vortice.Direct3D12.HeapProperties(Vortice.Direct3D12.HeapType.Default),
                Vortice.Direct3D12.HeapFlags.None,
                Vortice.Direct3D12.ResourceDescription.Buffer(new Vortice.Direct3D12.ResourceAllocationInfo { Alignment = 65536, SizeInBytes = size }, flags),
                initialState);
        }

        public static T Name<T>(this T child, string name)
            where T : Vortice.Direct3D12.ID3D12Object
        {
            child.Name = name;
            return child;
        }
    }
}
