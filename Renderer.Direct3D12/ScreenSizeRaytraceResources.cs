using Data.Space;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    internal class ScreenSizeRaytraceResources : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();

        public ScreenSizeRaytraceResources(Vortice.Direct3D12.ID3D12Device10 device, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            ResourcePool = disposeTracker.Track(new ResourcePool(device));

            var numElements = (uint)(screenSize.Width * screenSize.Height);

            FrameDataKey = new GBufferKey
            {
                HeapType = Vortice.Direct3D12.HeapType.Default,
                InitialLayout = Vortice.Direct3D12.BarrierLayout.Undefined,
                Description = new Vortice.Direct3D12.ResourceDescription1
                {
                    SampleDescription = new Vortice.DXGI.SampleDescription { Count = 1, Quality = 0 },
                    DepthOrArraySize = 1,
                    Dimension = Vortice.Direct3D12.ResourceDimension.Buffer,
                    Format = Vortice.DXGI.Format.Unknown,
                    MipLevels = 1,
                    Height = 1,
                    Width = numElements * (ulong)Marshal.SizeOf<Shaders.Data.RaytracingOutputData>(),
                    Layout = Vortice.Direct3D12.TextureLayout.RowMajor,
                    Flags = Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess
                },
                UAV = new Vortice.Direct3D12.UnorderedAccessViewDescription
                {
                    ViewDimension = Vortice.Direct3D12.UnorderedAccessViewDimension.Buffer,
                    Buffer = new Vortice.Direct3D12.BufferUnorderedAccessView
                    {
                        StructureByteStride = (uint)Marshal.SizeOf<Shaders.Data.RaytracingOutputData>(),
                        FirstElement = 0,
                        NumElements = numElements
                    }
                }
            };

            FrameTextureKey = new IlluminanceTextureKey
            {
                HeapType = Vortice.Direct3D12.HeapType.Default,
                InitialLayout = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                Description = new Vortice.Direct3D12.ResourceDescription1
                {
                    SampleDescription = new Vortice.DXGI.SampleDescription { Count = 1, Quality = 0 },
                    DepthOrArraySize = 1,
                    Dimension = Vortice.Direct3D12.ResourceDimension.Texture2D,
                    Format = renderTargetFormat,
                    MipLevels = 1,
                    Height = screenSize.Height,
                    Width = screenSize.Width,
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
                InitialLayout = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                Description = new Vortice.Direct3D12.ResourceDescription1
                {
                    SampleDescription = new Vortice.DXGI.SampleDescription { Count = 1, Quality = 0 },
                    DepthOrArraySize = 1,
                    Dimension = Vortice.Direct3D12.ResourceDimension.Texture2D,
                    Format = Vortice.DXGI.Format.R32G32_UInt,
                    MipLevels = 1,
                    Height = screenSize.Height,
                    Width = screenSize.Width,
                    Layout = Vortice.Direct3D12.TextureLayout.Unknown,
                    Flags = Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess
                },
                UAV = new Vortice.Direct3D12.UnorderedAccessViewDescription
                {
                    ViewDimension = Vortice.Direct3D12.UnorderedAccessViewDimension.Texture2D
                }
            };

            VarianceTextureKey = new VarianceDataTextureKey
            {
                HeapType = Vortice.Direct3D12.HeapType.Default,
                InitialLayout = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                Description = new Vortice.Direct3D12.ResourceDescription1
                {
                    SampleDescription = new Vortice.DXGI.SampleDescription { Count = 1, Quality = 0 },
                    DepthOrArraySize = 1,
                    Dimension = Vortice.Direct3D12.ResourceDimension.Texture2D,
                    Format = Vortice.DXGI.Format.R32G32B32A32_Float,
                    MipLevels = 1,
                    Height = screenSize.Height,
                    Width = screenSize.Width,
                    Layout = Vortice.Direct3D12.TextureLayout.Unknown,
                    Flags = Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess
                },
                UAV = new Vortice.Direct3D12.UnorderedAccessViewDescription
                {
                    ViewDimension = Vortice.Direct3D12.UnorderedAccessViewDimension.Texture2D
                }
            };

            ImageMeanTexture = ResourcePool.LeaseResource(VarianceTextureKey, "Image mean texture");
            ImageStdDevTexture = ResourcePool.LeaseResource(VarianceTextureKey, "Image std dev texture");
        }

        public ResourcePool ResourcePool { get; }
        public GBufferKey FrameDataKey { get; }
        public IlluminanceTextureKey FrameTextureKey { get; }
        public AtrousDataTextureKey AtrousTextureKey { get; }
        public VarianceDataTextureKey VarianceTextureKey { get; }

        public ResourcePool.ResourceLifetime<VarianceDataTextureKey> ImageMeanTexture { get; }
        public ResourcePool.ResourceLifetime<VarianceDataTextureKey> ImageStdDevTexture { get; }
        public ResourcePool.ResourceLifetime<IlluminanceTextureKey>? PreviousFrameIlluminance { get; set; }

        public void Dispose() => disposeTracker.Dispose();

        public class GBufferKey : ResourcePool.UAVResourceKey { }
        public class IlluminanceTextureKey : ResourcePool.UAVResourceKey { }
        public class AtrousDataTextureKey : ResourcePool.UAVResourceKey { }
        public class VarianceDataTextureKey : ResourcePool.UAVResourceKey { }
    }
}
