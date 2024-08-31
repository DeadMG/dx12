using Data.Space;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12.Shaders
{
    internal class ScreenSizeRaytraceResources : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();

        public ScreenSizeRaytraceResources(Vortice.Direct3D12.ID3D12Device5 device, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            ResourcePool = disposeTracker.Track(new ResourcePool(device));

            var numElements = screenSize.Width * screenSize.Height;

            FrameDataKey = new GBufferKey
            {
                HeapType = Vortice.Direct3D12.HeapType.Default,
                InitialState = Vortice.Direct3D12.ResourceStates.UnorderedAccess,
                Description = new Vortice.Direct3D12.ResourceDescription
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
                },
                UAV = new Vortice.Direct3D12.UnorderedAccessViewDescription
                {
                    ViewDimension = Vortice.Direct3D12.UnorderedAccessViewDimension.Buffer,
                    Buffer = new Vortice.Direct3D12.BufferUnorderedAccessView
                    {
                        StructureByteStride = Marshal.SizeOf<Data.RaytracingOutputData>(),
                        FirstElement = 0,
                        NumElements = numElements
                    }
                }
            };

            FrameTextureKey = new IlluminanceTextureKey
            {
                HeapType = Vortice.Direct3D12.HeapType.Default,
                InitialState = Vortice.Direct3D12.ResourceStates.CopySource,
                Description = new Vortice.Direct3D12.ResourceDescription
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
                },
                UAV = new Vortice.Direct3D12.UnorderedAccessViewDescription
                {
                    ViewDimension = Vortice.Direct3D12.UnorderedAccessViewDimension.Texture2D
                }
            };

            AtrousTextureKey = new AtrousDataTextureKey
            {
                HeapType = Vortice.Direct3D12.HeapType.Default,
                InitialState = Vortice.Direct3D12.ResourceStates.CopySource,
                Description = new Vortice.Direct3D12.ResourceDescription
                {
                    SampleDescription = new Vortice.DXGI.SampleDescription { Count = 1, Quality = 0 },
                    DepthOrArraySize = 1,
                    Dimension = Vortice.Direct3D12.ResourceDimension.Texture2D,
                    Format = Vortice.DXGI.Format.R32G32_UInt,
                    MipLevels = 1,
                    Height = screenSize.Height,
                    Width = (ulong)screenSize.Width,
                    Layout = Vortice.Direct3D12.TextureLayout.Unknown,
                    Flags = Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess
                },
                UAV = new Vortice.Direct3D12.UnorderedAccessViewDescription
                {
                    ViewDimension = Vortice.Direct3D12.UnorderedAccessViewDimension.Texture2D
                }
            };
        }

        public ResourcePool ResourcePool { get; }
        public GBufferKey FrameDataKey { get; }
        public IlluminanceTextureKey FrameTextureKey { get; }
        public AtrousDataTextureKey AtrousTextureKey { get; }

        public void Dispose() => disposeTracker.Dispose();

        public class GBufferKey : ResourcePool.UAVResourceKey { }
        public class IlluminanceTextureKey : ResourcePool.UAVResourceKey { }
        public class AtrousDataTextureKey : ResourcePool.UAVResourceKey { }
    }
}
