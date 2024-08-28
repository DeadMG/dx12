using Data.Space;
using Simulation.Physics;
using System.Numerics;
using Util;

namespace Renderer.Direct3D12.Shaders
{
    internal class CameraRayGen : IRaytracingPipelineStep
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Raytrace.RayGen.Filter filterShader;
        private readonly Raytrace.RayGen.Camera cameraShader;
        private readonly Raytrace.RayGen.Atrous atrousShader;

        public CameraRayGen(Shaders.Raytrace.RayGen.Filter filterShader, Shaders.Raytrace.RayGen.Camera cameraShader, Shaders.Raytrace.RayGen.Atrous atrousShader)
        {
            this.cameraShader = cameraShader;
            this.filterShader = filterShader;
            this.atrousShader = atrousShader;
        }

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects() => [];

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public void PrepareRaytracing(RaytracePreparation preparation)
        {
            var frustum = Frustum.FromScreen(new ScreenRectangle { Start = new ScreenPosition(0, 0), End = new ScreenPosition(preparation.Camera.ScreenSize.Width, preparation.Camera.ScreenSize.Height) }, preparation.Camera.ScreenSize, preparation.Camera.InvViewProjection);

            preparation.ShaderTable.AddRayGeneration(cameraShader.Export, tlas => new Data.CameraMatrices
            {
                WorldBottomLeft = frustum.Points[0],
                WorldTopLeft = frustum.Points[1],
                WorldTopRight = frustum.Points[2],
                Origin = preparation.Camera.Position,
                OutputIndex = preparation.HeapAccumulator.AddUAV(preparation.RayGenSrv),
                SceneBVHIndex = preparation.HeapAccumulator.AddRaytracingStructure(tlas),
            }.GetBytes());
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
            commit.List.List.ResourceBarrier([
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(commit.FilterSrv, Vortice.Direct3D12.ResourceStates.CopySource, Vortice.Direct3D12.ResourceStates.UnorderedAccess))
            ]);

            commit.List.List.ResourceBarrierUnorderedAccessView(commit.RayGenSrv);
            commit.List.List.ResourceBarrierUnorderedAccessView(commit.Frames[0].Data.Buffer);
            
            commit.List.List.SetPipelineState(atrousShader.PipelineState);
            commit.List.List.SetComputeRootSignature(atrousShader.RootSignature);

            var inputDataIndex = commit.HeapAccumulator.AddStructuredBuffer(commit.Frames[0].Data);
            var inputTextureIndex = commit.HeapAccumulator.AddUAV(commit.RayGenSrv);
            var outputTextureIndex = commit.HeapAccumulator.AddUAV(commit.Frames[0].OutputSrv);

            for (int i = 0; i < 3; i++)
            {
                var stepFactor = (float)Math.Pow(2, -i);

                commit.List.List.SetComputeRoot32BitConstants(0, [new Data.AtrousRootParameters
                {
                    ImageHeight = (uint)commit.ScreenSize.Height,
                    ImageWidth = (uint)commit.ScreenSize.Width,
                    StepWidth = i,
                    CPhi = stepFactor * 1.0f,
                    NPhi = 1.0f,
                    PPhi = stepFactor * 1.0f,
                    InputTextureIndex = inputTextureIndex,
                    OutputTextureIndex = outputTextureIndex,
                    InputDataIndex = inputDataIndex,
                }]);
                commit.List.List.Dispatch((int)Math.Ceiling(commit.ScreenSize.Width / (float)32), (int)Math.Ceiling(commit.ScreenSize.Height / (float)32), 1);

                commit.List.List.ResourceBarrierUnorderedAccessView(commit.RayGenSrv);
                commit.List.List.ResourceBarrierUnorderedAccessView(commit.Frames[0].OutputSrv);

                var input = inputTextureIndex;
                inputTextureIndex = outputTextureIndex;
                outputTextureIndex = input;
            }

            commit.List.List.SetPipelineState(filterShader.PipelineState);
            commit.List.List.SetComputeRootSignature(filterShader.RootSignature);
            commit.List.List.SetComputeRoot32BitConstants(0, [new Data.FilterParameters
            {
                ImageHeight = (uint)commit.ScreenSize.Height,
                ImageWidth = (uint)commit.ScreenSize.Width,
                CurrentFrameIndex = commit.HeapAccumulator.AddUAV(commit.FilterSrv),
                CurrentDataIndex = inputDataIndex,
                PreviousFrameIndex = inputTextureIndex,
            }]);
            commit.List.List.Dispatch((int)Math.Ceiling(commit.ScreenSize.Width / (float)32), (int)Math.Ceiling(commit.ScreenSize.Height / (float)32), 1);

            commit.List.List.ResourceBarrier([
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(commit.FilterSrv, Vortice.Direct3D12.ResourceStates.UnorderedAccess, Vortice.Direct3D12.ResourceStates.CopySource)),
                new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(commit.RenderTarget, Vortice.Direct3D12.ResourceStates.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest))
            ]);

            commit.List.List.CopyResource(commit.RenderTarget, commit.FilterSrv);
            commit.List.List.ResourceBarrierTransition(commit.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest, Vortice.Direct3D12.ResourceStates.RenderTarget);
        }
    }
}
