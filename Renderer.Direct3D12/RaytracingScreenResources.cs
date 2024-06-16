using Data.Space;
using Util;

namespace Renderer.Direct3D12
{
    internal class RaytracingScreenResources : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12Resource outputSrv;

        public RaytracingScreenResources(Vortice.Direct3D12.ID3D12Device5 device, StateObjectProperties stateObjectProperties, Vortice.Direct3D12.ID3D12DescriptorHeap srvUavHeap, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            var outputDesc = new Vortice.Direct3D12.ResourceDescription
            {
                SampleDescription = new Vortice.DXGI.SampleDescription { Count = 1, Quality = 0 },
                DepthOrArraySize = 1,
                Dimension = Vortice.Direct3D12.ResourceDimension.Texture2D,
                Format = renderTargetFormat,
                MipLevels = 1,
                Height = screenSize.Height,
                Width = (ulong)screenSize.Width,
                Layout = Vortice.Direct3D12.TextureLayout.Unknown,
                Flags = Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess
            };

            outputSrv = disposeTracker.Track(device.CreateCommittedResource(Vortice.Direct3D12.HeapType.Default, outputDesc, Vortice.Direct3D12.ResourceStates.CopySource).Name("Raytrace Output UAV"));

            device.CreateUnorderedAccessView(outputSrv,
                null, 
                new Vortice.Direct3D12.UnorderedAccessViewDescription 
                { 
                    ViewDimension = Vortice.Direct3D12.UnorderedAccessViewDimension.Texture2D 
                }, 
                srvUavHeap.GetCPUDescriptorHandleForHeapStart());
        }

        public Vortice.Direct3D12.ID3D12Resource OutputSrv => outputSrv;

        public void Dispose() => disposeTracker.Dispose();
    }
}
