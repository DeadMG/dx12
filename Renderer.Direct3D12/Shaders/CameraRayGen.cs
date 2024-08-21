using Data.Space;
using Simulation.Physics;
using Util;

namespace Renderer.Direct3D12.Shaders
{
    internal class CameraRayGen : IRaytracingPipelineStep
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Raytrace.RayGen.Filter filterShader;
        private readonly Raytrace.RayGen.Camera cameraShader;

        private ScreenSize screenSize;

        public CameraRayGen(ScreenSize screenSize, Shaders.Raytrace.RayGen.Filter filterShader, Shaders.Raytrace.RayGen.Camera cameraShader)
        {
            this.cameraShader = cameraShader;
            this.filterShader = filterShader;

            this.screenSize = screenSize;
        }

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects() => [];

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public void PrepareRaytracing(RaytracePreparation preparation)
        {
            preparation.List.List.ResourceBarrier([
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(preparation.ScreenSizeRaytraceResources.FilterSrv, Vortice.Direct3D12.ResourceStates.CopySource, Vortice.Direct3D12.ResourceStates.UnorderedAccess))
            ]);

            var frustum = Frustum.FromScreen(new ScreenRectangle { Start = new ScreenPosition(0, 0), End = new ScreenPosition(preparation.Camera.ScreenSize.Width, preparation.Camera.ScreenSize.Height) }, preparation.Camera.ScreenSize, preparation.Camera.InvViewProjection);

            preparation.ShaderTable.AddRayGeneration(cameraShader.Export, tlas => new Data.CameraMatrices
            {
                WorldBottomLeft = frustum.Points[0],
                WorldTopLeft = frustum.Points[1],
                WorldTopRight = frustum.Points[2],
                Origin = preparation.Camera.Position,
                OutputIndex = preparation.HeapAccumulator.AddUAV(preparation.ScreenSizeRaytraceResources.OutputSrv),
                SceneBVHIndex = preparation.HeapAccumulator.AddRaytracingStructure(tlas),
                PreviousIndex = preparation.HeapAccumulator.AddUAV(preparation.ScreenSizeRaytraceResources.FilterSrv),
            }.GetBytes());
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
            commit.List.List.ResourceBarrierUnorderedAccessView(commit.ScreenSizeRaytraceResources.OutputSrv);
            commit.List.List.ResourceBarrierUnorderedAccessView(commit.ScreenSizeRaytraceResources.Data);

            // Do stuff
            commit.List.List.SetPipelineState(filterShader.PipelineState);
            commit.List.List.SetComputeRootSignature(filterShader.RootSignature);
            commit.List.List.SetComputeRoot32BitConstants(0, [new Data.FilterParameters
            {
                ImageHeight = (uint)screenSize.Height,
                ImageWidth = (uint)screenSize.Width,
                DataIndex = commit.HeapAccumulator.AddStructuredBuffer(commit.ScreenSizeRaytraceResources.Data, commit.ScreenSizeRaytraceResources.DataSrv),
                InputIndex = commit.HeapAccumulator.AddUAV(commit.ScreenSizeRaytraceResources.OutputSrv),
                OutputIndex = commit.HeapAccumulator.AddUAV(commit.ScreenSizeRaytraceResources.FilterSrv),
            }]);
            commit.List.List.Dispatch((int)Math.Ceiling(screenSize.Width / (float)32), (int)Math.Ceiling(screenSize.Height / (float)32), 1);

            commit.List.List.ResourceBarrier([
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(commit.ScreenSizeRaytraceResources.FilterSrv, Vortice.Direct3D12.ResourceStates.UnorderedAccess, Vortice.Direct3D12.ResourceStates.CopySource)),
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(commit.RenderTarget, Vortice.Direct3D12.ResourceStates.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest))
            ]);

            commit.List.List.CopyResource(commit.RenderTarget, commit.ScreenSizeRaytraceResources.FilterSrv);
            commit.List.List.ResourceBarrierTransition(commit.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest, Vortice.Direct3D12.ResourceStates.RenderTarget);
        }
    }
}
