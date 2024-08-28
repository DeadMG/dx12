using Data.Space;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12.Shaders
{
    internal class ScreenSizeRaytraceResources : IDisposable
    {
        private const int historyFrames = 4;
        private const int totalFrames = historyFrames + 1;

        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly List<FrameData> frameData = new List<FrameData>();
        private readonly Vortice.Direct3D12.ID3D12Resource filteredSrv;
        private readonly Vortice.Direct3D12.ID3D12Resource rayGenSrv;
        private int currentFrame = 0;

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

            filteredSrv = disposeTracker.Track(device.CreateCommittedResource(Vortice.Direct3D12.HeapType.Default, outputDesc, Vortice.Direct3D12.ResourceStates.CopySource)).Name("Filter output UAV");
            rayGenSrv = disposeTracker.Track(device.CreateCommittedResource(Vortice.Direct3D12.HeapType.Default, outputDesc, Vortice.Direct3D12.ResourceStates.UnorderedAccess)).Name("Raytrace output UAV");

            for (int i = 0; i < totalFrames; i++)
            {
                var outputSrv = disposeTracker.Track(device.CreateCommittedResource(Vortice.Direct3D12.HeapType.Default, outputDesc, Vortice.Direct3D12.ResourceStates.UnorderedAccess)).Name("Atrous Output UAV");

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
                var dataBuffer = new StructuredBuffer(
                    disposeTracker.Track(device.CreateCommittedResource(Vortice.Direct3D12.HeapType.Default, dataDesc, Vortice.Direct3D12.ResourceStates.UnorderedAccess)).Name("Raytrace data UAV"),
                    new Vortice.Direct3D12.BufferShaderResourceView { StructureByteStride = Marshal.SizeOf<Data.RaytracingOutputData>(), NumElements = numElements });

                frameData.Add(new FrameData { OutputSrv = outputSrv, Data = dataBuffer, InverseViewProjectionMatrix = null, ViewProjectionMatrix = null });
            }
        }

        public Vortice.Direct3D12.ID3D12Resource RayGenSrv => rayGenSrv;
        public Vortice.Direct3D12.ID3D12Resource FilterSrv => filteredSrv;

        public List<FrameData> RetrieveFrames(Matrix4x4 viewProjectionMatrix, Matrix4x4 inverseViewProjectionMatrix)
        {
            frameData[currentFrame].InverseViewProjectionMatrix = inverseViewProjectionMatrix;
            frameData[currentFrame].ViewProjectionMatrix = viewProjectionMatrix;

            var result = Enumerable.Range(currentFrame - totalFrames, totalFrames)
                .Select(x => x < 0 ? x + totalFrames : x)
                .Select(i => frameData[i])
                .Where(i => i.InverseViewProjectionMatrix != null)
                .Reverse()
                .ToList();

            currentFrame = (currentFrame + 1) % totalFrames;

            return result;
        }

        public void Dispose() => disposeTracker.Dispose();
    }

    internal class FrameData
    {
        public required Matrix4x4? InverseViewProjectionMatrix { get; set; }
        public required Matrix4x4? ViewProjectionMatrix { get; set; }
        public required StructuredBuffer Data { get; init; }
        public required Vortice.Direct3D12.ID3D12Resource OutputSrv { get; init; }
    }
}
