﻿using Data.Space;
using Simulation.Physics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Util;

namespace Renderer.Direct3D12.Shaders
{
    internal class CameraRayGen : IRaytracingPipelineStep
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12Device5 device;
        private readonly Vortice.DXGI.Format renderTargetFormat;
        private readonly Raytrace.RayGen.Filter filterShader;
        private readonly Raytrace.RayGen.Camera cameraShader;

        private RaytracingScreenResources screenResources;
        private ScreenSize screenSize;

        public CameraRayGen(Vortice.Direct3D12.ID3D12Device5 device, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat, Shaders.Raytrace.RayGen.Filter filterShader, Shaders.Raytrace.RayGen.Camera cameraShader)
        {
            this.cameraShader = cameraShader;
            this.filterShader = filterShader;
            this.device = device;
            this.renderTargetFormat = renderTargetFormat;

            this.screenSize = screenSize;
            this.screenResources = new RaytracingScreenResources(device, screenSize, renderTargetFormat); 
        }

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects() => [];

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
                screenResources = new RaytracingScreenResources(device, preparation.Camera.ScreenSize, renderTargetFormat);
            }

            preparation.List.List.ResourceBarrier([
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(screenResources.FilterSrv, Vortice.Direct3D12.ResourceStates.CopySource, Vortice.Direct3D12.ResourceStates.UnorderedAccess))
            ]);

            var frustum = Frustum.FromScreen(new ScreenRectangle { Start = new ScreenPosition(0, 0), End = new ScreenPosition(preparation.Camera.ScreenSize.Width, preparation.Camera.ScreenSize.Height) }, preparation.Camera.ScreenSize, preparation.Camera.InvViewProjection);

            preparation.ShaderTable.AddRayGeneration(cameraShader.Export, tlas => new Data.CameraMatrices
            {
                WorldBottomLeft = frustum.Points[0],
                WorldTopLeft = frustum.Points[1],
                WorldTopRight = frustum.Points[2],
                Origin = preparation.Camera.Position,
                DataIndex = preparation.HeapAccumulator.AddStructuredBuffer(screenResources.Data, screenResources.DataSrv),
                OutputIndex = preparation.HeapAccumulator.AddUAV(screenResources.OutputSrv),
                SceneBVHIndex = preparation.HeapAccumulator.AddRaytracingStructure(tlas),
                PreviousIndex = preparation.HeapAccumulator.AddUAV(screenResources.FilterSrv),
            }.GetBytes());
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
            commit.List.List.ResourceBarrierUnorderedAccessView(screenResources.OutputSrv);
            commit.List.List.ResourceBarrierUnorderedAccessView(screenResources.Data);

            // Do stuff
            commit.List.List.SetPipelineState(filterShader.PipelineState);
            commit.List.List.SetComputeRootSignature(filterShader.RootSignature);
            commit.List.List.SetComputeRoot32BitConstants(0, [new Data.FilterParameters
            {
                ImageHeight = (uint)screenSize.Height,
                ImageWidth = (uint)screenSize.Width,
                DataIndex = commit.HeapAccumulator.AddStructuredBuffer(screenResources.Data, screenResources.DataSrv),
                InputIndex = commit.HeapAccumulator.AddUAV(screenResources.OutputSrv),
                OutputIndex = commit.HeapAccumulator.AddUAV(screenResources.FilterSrv),
            }]);
            commit.List.List.Dispatch((int)Math.Ceiling(screenSize.Width / (float)32), (int)Math.Ceiling(screenSize.Height / (float)32), 1);

            commit.List.List.ResourceBarrier([
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(screenResources.FilterSrv, Vortice.Direct3D12.ResourceStates.UnorderedAccess, Vortice.Direct3D12.ResourceStates.CopySource)),
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(commit.RenderTarget, Vortice.Direct3D12.ResourceStates.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest))
            ]);

            commit.List.List.CopyResource(commit.RenderTarget, screenResources.FilterSrv);
            commit.List.List.ResourceBarrierTransition(commit.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest, Vortice.Direct3D12.ResourceStates.RenderTarget);
        }

        internal class RaytracingScreenResources : IDisposable
        {
            private readonly DisposeTracker disposeTracker = new DisposeTracker();
            private readonly Vortice.Direct3D12.ID3D12Resource dataBuffer;
            private readonly Vortice.Direct3D12.ID3D12Resource outputSrv;
            private readonly Vortice.Direct3D12.ID3D12Resource filteredSrv;

            public RaytracingScreenResources(Vortice.Direct3D12.ID3D12Device5 device, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
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
}
