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

        private readonly Vortice.Direct3D12.ID3D12RootSignature filterSignature;
        private readonly Vortice.Direct3D12.ID3D12PipelineState pipelineState;
        private readonly Vortice.DXGI.Format renderTargetFormat;
       
        private RaytracingScreenResources screenResources;
        private ScreenSize screenSize;

        public Camera(Vortice.Direct3D12.ID3D12Device5 device, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            this.device = device;
            this.renderTargetFormat = renderTargetFormat;

            srvUavHeap = disposeTracker.Track(device.CreateDescriptorHeap(new Vortice.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = 3,
                Flags = Vortice.Direct3D12.DescriptorHeapFlags.ShaderVisible,
                NodeMask = 0,
                Type = Vortice.Direct3D12.DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            }).Name("RayGen SRV/UAV heap"));

            this.screenSize = screenSize;
            this.screenResources = new RaytracingScreenResources(device, srvUavHeap, screenSize, renderTargetFormat);

            var tableParameter = new Vortice.Direct3D12.RootParameter1(
                new Vortice.Direct3D12.RootDescriptorTable1(
                    new Vortice.Direct3D12.DescriptorRange1(Vortice.Direct3D12.DescriptorRangeType.UnorderedAccessView, 2, 0),
                    new Vortice.Direct3D12.DescriptorRange1(Vortice.Direct3D12.DescriptorRangeType.ShaderResourceView, 1, 0)),
                Vortice.Direct3D12.ShaderVisibility.All);

            var cameraParameters = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<CameraParameters>() / 4), Vortice.Direct3D12.ShaderVisibility.All);
            var filterParameters = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<FilterParameters>() / 4), Vortice.Direct3D12.ShaderVisibility.All);

            filterSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.None, [tableParameter, filterParameters]))).Name("Filter signature");
            signature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [tableParameter, cameraParameters])).Name("RayGen signature"));
            pipelineState = disposeTracker.Track(device.CreateComputePipelineState(new Vortice.Direct3D12.ComputePipelineStateDescription
            {
                ComputeShader = Shader.LoadDxil("Shaders/Raytrace/RayGen/Filter.hlsl", "cs_6_3", "bilteralFilter"),
                RootSignature = filterSignature,
                Flags = Vortice.Direct3D12.PipelineStateFlags.None,
                NodeMask = 0
            }));
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

            preparation.List.List.ResourceBarrier([
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(screenResources.FilterSrv, Vortice.Direct3D12.ResourceStates.CopySource, Vortice.Direct3D12.ResourceStates.UnorderedAccess))
            ]);

            preparation.DescriptorHeaps.Add(srvUavHeap);
            preparation.ShaderTable.AddRayGeneration("RayGen", tlas => BitConverter.GetBytes(srvUavHeap.GetGPUDescriptorHandleForHeapStart()).Concat(cameraData.GetBytes()).ToArray());
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
                srvUavHeap.CPU(2));
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
            var sigmaD = 5;

            commit.List.List.ResourceBarrierUnorderedAccessView(screenResources.OutputSrv);

            // Do stuff
            commit.List.List.SetPipelineState(pipelineState);
            commit.List.List.SetComputeRootSignature(filterSignature);
            commit.List.List.SetComputeRootDescriptorTable(0, srvUavHeap.GetGPUDescriptorHandleForHeapStart());
            commit.List.List.SetComputeRoot32BitConstants<FilterParameters>(1, [new FilterParameters
            {
                KernelWidth = sigmaD,
                KernelHeight = sigmaD,
                SigmaD = 2 * (float)Math.Pow(sigmaD, 2),
                SigmaR = 2 * (float)Math.Pow(10, 2),
                ImageHeight = screenSize.Height,
                ImageWidth = screenSize.Width,
            }]);
            commit.List.List.Dispatch((int)Math.Ceiling(screenSize.Width / (float)32), (int)Math.Ceiling(screenSize.Height / (float)32), 1);

            commit.List.List.ResourceBarrier([
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(screenResources.FilterSrv, Vortice.Direct3D12.ResourceStates.UnorderedAccess, Vortice.Direct3D12.ResourceStates.CopySource)),
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(commit.RenderTarget, Vortice.Direct3D12.ResourceStates.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest))
            ]);

            commit.List.List.CopyResource(commit.RenderTarget, screenResources.FilterSrv);
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

        [StructLayout(LayoutKind.Explicit)]
        private struct FilterParameters
        {
            [FieldOffset(0)]
            public int KernelWidth;

            [FieldOffset(4)]
            public int KernelHeight;

            [FieldOffset(8)]
            public int ImageWidth;

            [FieldOffset(12)]
            public int ImageHeight;

            [FieldOffset(16)]
            public float SigmaD;

            [FieldOffset(20)]
            public float SigmaR;

            [FieldOffset(24)]
            public bool SoftShadows;
        }

        internal class RaytracingScreenResources : IDisposable
        {
            private readonly DisposeTracker disposeTracker = new DisposeTracker();
            private readonly Vortice.Direct3D12.ID3D12Resource outputSrv;
            private readonly Vortice.Direct3D12.ID3D12Resource filteredSrv;

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
                outputDesc.Format = renderTargetFormat;
                filteredSrv = disposeTracker.Track(device.CreateCommittedResource(Vortice.Direct3D12.HeapType.Default, outputDesc, Vortice.Direct3D12.ResourceStates.CopySource)).Name("Filter output UAV");

                device.CreateUnorderedAccessView(outputSrv,
                    null,
                    new Vortice.Direct3D12.UnorderedAccessViewDescription
                    {
                        ViewDimension = Vortice.Direct3D12.UnorderedAccessViewDimension.Texture2D
                    },
                    srvUavHeap.GetCPUDescriptorHandleForHeapStart());

                device.CreateUnorderedAccessView(filteredSrv,
                    null,
                    new Vortice.Direct3D12.UnorderedAccessViewDescription
                    {
                        ViewDimension = Vortice.Direct3D12.UnorderedAccessViewDimension.Texture2D
                    },
                    srvUavHeap.CPU(1));
            }

            public Vortice.Direct3D12.ID3D12Resource OutputSrv => outputSrv;
            public Vortice.Direct3D12.ID3D12Resource FilterSrv => filteredSrv;

            public void Dispose() => disposeTracker.Dispose();
        }
    }
}
