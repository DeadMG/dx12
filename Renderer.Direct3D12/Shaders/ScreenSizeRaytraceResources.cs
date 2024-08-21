using Data.Space;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12.Shaders
{
    internal class ScreenSizeRaytraceResources : IDisposable
    {
         private readonly DisposeTracker disposeTracker = new DisposeTracker();
         private readonly Vortice.Direct3D12.ID3D12Resource dataBuffer;
         private readonly Vortice.Direct3D12.ID3D12Resource outputSrv;
         private readonly Vortice.Direct3D12.ID3D12Resource filteredSrv;

         public ScreenSizeRaytraceResources(Vortice.Direct3D12.ID3D12Device5 device, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
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
             outputDesc.Format = renderTargetFormat;
             filteredSrv = disposeTracker.Track(device.CreateCommittedResource(Vortice.Direct3D12.HeapType.Default, outputDesc, Vortice.Direct3D12.ResourceStates.CopySource)).Name("Filter output UAV");

             var numElements = screenSize.Width * screenSize.Height;
             var dataDesc = new Vortice.Direct3D12.ResourceDescription
             {
                 SampleDescription = new Vortice.DXGI.SampleDescription { Count = 1, Quality = 0 },
                 DepthOrArraySize = 1,
                 Dimension = Vortice.Direct3D12.ResourceDimension.Buffer,
                 Format = Vortice.DXGI.Format.Unknown,
                 MipLevels = 1,
                 Height = 1,
                 Width = (ulong)numElements * (ulong)Marshal.SizeOf<Data.RaytracingOutputData>(),
                 Layout = Vortice.Direct3D12.TextureLayout.RowMajor,
                 Flags = Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess
             };
             DataSrv = new Vortice.Direct3D12.BufferShaderResourceView { StructureByteStride = Marshal.SizeOf<Data.RaytracingOutputData>(), NumElements = numElements };
             dataBuffer = disposeTracker.Track(device.CreateCommittedResource(Vortice.Direct3D12.HeapType.Default, dataDesc, Vortice.Direct3D12.ResourceStates.UnorderedAccess)).Name("Raytrace data UAV");
         }

         public Vortice.Direct3D12.ID3D12Resource OutputSrv => outputSrv;
         public Vortice.Direct3D12.ID3D12Resource FilterSrv => filteredSrv;
         public Vortice.Direct3D12.ID3D12Resource Data => dataBuffer;
         public Vortice.Direct3D12.BufferShaderResourceView DataSrv { get; private set; }

         public void Dispose() => disposeTracker.Dispose();
    }
}
