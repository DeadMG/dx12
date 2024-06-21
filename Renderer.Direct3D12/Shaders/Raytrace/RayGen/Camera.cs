using Data.Space;
using Simulation.Physics;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12.Shaders.Raytrace.RayGen
{
    internal class Camera : IShader
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12DescriptorHeap srvUavHeap;
        private readonly Vortice.Direct3D12.ID3D12RootSignature signature;
        private readonly Vortice.Direct3D12.ID3D12Device5 device;
        private readonly Vortice.DXGI.Format renderTargetFormat;

        private RaytracingScreenResources screenResources;
        private ScreenSize screenSize;

        public Camera(Vortice.Direct3D12.ID3D12Device5 device, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            this.device = device;
            this.renderTargetFormat = renderTargetFormat;

            srvUavHeap = disposeTracker.Track(device.CreateDescriptorHeap(new Vortice.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = 2,
                Flags = Vortice.Direct3D12.DescriptorHeapFlags.ShaderVisible,
                NodeMask = 0,
                Type = Vortice.Direct3D12.DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            }).Name("RayGen SRV/UAV heap"));

            this.screenSize = screenSize;
            this.screenResources = new RaytracingScreenResources(device, srvUavHeap, screenSize, renderTargetFormat);

            var tableParameter = new Vortice.Direct3D12.RootParameter1(
                new Vortice.Direct3D12.RootDescriptorTable1(
                    new Vortice.Direct3D12.DescriptorRange1(Vortice.Direct3D12.DescriptorRangeType.UnorderedAccessView, 1, 0),
                    new Vortice.Direct3D12.DescriptorRange1(Vortice.Direct3D12.DescriptorRangeType.ShaderResourceView, 1, 0)),
                Vortice.Direct3D12.ShaderVisibility.All);

            var cameraParameters = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<CameraParameters>() / 4), Vortice.Direct3D12.ShaderVisibility.All);

            signature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [tableParameter, cameraParameters])).Name("RayGen signature"));
        }

        private readonly ReadOnlyMemory<byte> dxil = Shader.LoadDxil("Shaders/Raytrace/RayGen/Camera.hlsl", "lib_6_3");

        public string[] Exports => ["RayGen"];

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects()
        {
            var signatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(signature));

            return [
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(dxil,
                     Exports.Select(x => new Vortice.Direct3D12.ExportDescription(x)).ToArray())),
                signatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(signatureSubobject, Exports))
            ];
        }

        public void Dispose()
        {
            screenResources.Dispose();
            disposeTracker.Dispose();
        }

        public void PrepareRaytracing(RaytracePreparation preparation)
        {
            if (preparation.Camera.ScreenSize != screenSize)
            {
                screenSize = preparation.Camera.ScreenSize;
                screenResources.Dispose();
                screenResources = new RaytracingScreenResources(device, srvUavHeap, preparation.Camera.ScreenSize, renderTargetFormat);
            }

            var frustum = Frustum.FromScreen(new ScreenRectangle { Start = new ScreenPosition(0, 0), End = new ScreenPosition(preparation.Camera.ScreenSize.Width, preparation.Camera.ScreenSize.Height) }, preparation.Camera.ScreenSize, preparation.Camera.InvViewProjection);
            var cameraData = new CameraParameters { worldBottomLeft = frustum.Points[0], worldTopLeft = frustum.Points[1], worldTopRight = frustum.Points[2], Origin = preparation.Camera.Position };

            preparation.DescriptorHeaps.Add(srvUavHeap);
            preparation.ShaderTable.AddRayGeneration("RayGen", tlas => BitConverter.GetBytes(srvUavHeap.GetGPUDescriptorHandleForHeapStart()).Concat(cameraData.GetBytes()).ToArray());
            preparation.List.List.ResourceBarrierTransition(screenResources.OutputSrv, Vortice.Direct3D12.ResourceStates.CopySource, Vortice.Direct3D12.ResourceStates.UnorderedAccess);
        }

        public void FinaliseRaytracing(RaytraceFinalisation finalisation)
        {
            device.CreateShaderResourceView(null,
                new Vortice.Direct3D12.ShaderResourceViewDescription
                {
                    Format = Vortice.DXGI.Format.Unknown,
                    ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.RaytracingAccelerationStructure,
                    Shader4ComponentMapping = Vortice.Direct3D12.ShaderComponentMapping.Default,
                    RaytracingAccelerationStructure = new Vortice.Direct3D12.RaytracingAccelerationStructureShaderResourceView
                    {
                        Location = finalisation.TLAS.GPUVirtualAddress,
                    }
                },
                srvUavHeap.CPU(1));
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
            commit.List.List.ResourceBarrier([
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(screenResources.OutputSrv, Vortice.Direct3D12.ResourceStates.UnorderedAccess, Vortice.Direct3D12.ResourceStates.CopySource)),
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(commit.RenderTarget, Vortice.Direct3D12.ResourceStates.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest))
            ]);

            commit.List.List.CopyResource(commit.RenderTarget, screenResources.OutputSrv);
            commit.List.List.ResourceBarrierTransition(commit.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest, Vortice.Direct3D12.ResourceStates.RenderTarget);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct CameraParameters
        {
            [FieldOffset(0)]
            public Vector3 worldTopLeft;

            [FieldOffset(16)]
            public Vector3 worldTopRight;

            [FieldOffset(32)]
            public Vector3 worldBottomLeft;

            [FieldOffset(48)]
            public Vector3 Origin;
        }

        internal class RaytracingScreenResources : IDisposable
        {
            private readonly DisposeTracker disposeTracker = new DisposeTracker();
            private readonly Vortice.Direct3D12.ID3D12Resource outputSrv;

            public RaytracingScreenResources(Vortice.Direct3D12.ID3D12Device5 device, Vortice.Direct3D12.ID3D12DescriptorHeap srvUavHeap, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
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
}
