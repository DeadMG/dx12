using Data.Space;
using Renderer.Direct3D12.Shaders;
using Simulation;
using Util;

namespace Renderer.Direct3D12
{
    internal class RaytracingVolumeRenderer : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private const int maxRays = 2;

        private readonly CommandListPool directListPool;
        private readonly Vortice.Direct3D12.ID3D12Device5 device;

        private readonly Vortice.Direct3D12.ID3D12RootSignature emptyGlobalSignature;
        private readonly Vortice.Direct3D12.ID3D12StateObject state;
        private readonly StateObjectProperties stateObjectProperties;

        private readonly Shaders.Raytrace.Hit.Object objectShader;
        private readonly Shaders.Raytrace.Miss.Black missShader;
        private readonly Shaders.Raytrace.RayGen.Camera rayGenShader;

        public RaytracingVolumeRenderer(MeshResourceCache meshResourceCache, Vortice.Direct3D12.ID3D12Device5 device, CommandListPool directListPool, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            this.directListPool = directListPool;
            this.device = device;

            objectShader = disposeTracker.Track(new Shaders.Raytrace.Hit.Object(device, meshResourceCache, maxRays));
            missShader = disposeTracker.Track(new Shaders.Raytrace.Miss.Black(device));
            rayGenShader = disposeTracker.Track(new Shaders.Raytrace.RayGen.Camera(device, screenSize, renderTargetFormat));

            emptyGlobalSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1())).Name("Empty global signature");

            var shaderConfigSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingShaderConfig { MaxAttributeSizeInBytes = 8, MaxPayloadSizeInBytes = 32 });

            Vortice.Direct3D12.StateSubObject[] fixedSubobjects = [
                shaderConfigSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(shaderConfigSubobject, Shaders.SelectMany(s => s.Exports).ToArray())),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingPipelineConfig(maxRays))
            ];

            state = disposeTracker.Track(device.CreateStateObject(new Vortice.Direct3D12.StateObjectDescription(Vortice.Direct3D12.StateObjectType.RaytracingPipeline,
                Shaders.SelectMany(s => s.CreateStateObjects()).Concat(fixedSubobjects).ToArray()))
                .Name("Raytrace state object"));

            stateObjectProperties = disposeTracker.Track(new StateObjectProperties(state));
        }

        private IShader[] Shaders => [objectShader, missShader, rayGenShader];

        public void Render(RendererParameters rp, Volume volume, Camera camera)
        {
            if (volume == null) return;

            var entry = directListPool.GetCommandList();

            var preparation = new RaytracePreparation 
            { 
                Camera = camera, 
                Volume = volume, 
                DescriptorHeaps = new List<Vortice.Direct3D12.ID3D12DescriptorHeap>(),
                InstanceDescriptions = new List<Vortice.Direct3D12.RaytracingInstanceDescription>(), 
                List = entry,
                ShaderTable = entry.DisposeAfterExecution(new ShaderBindingTable(stateObjectProperties)),
            };

            foreach (var shader in Shaders)
            {
                shader.PrepareRaytracing(preparation);
            }

            var finalise = new RaytraceFinalisation { TLAS = CreateTLAS(preparation) };
            foreach (var shader in Shaders)
            {
                shader.FinaliseRaytracing(finalise);
            }

            entry.List.SetComputeRootSignature(emptyGlobalSignature);
            entry.List.SetPipelineState1(state);
            entry.List.SetDescriptorHeaps(preparation.DescriptorHeaps.ToArray());

            var dispatchDesc = preparation.ShaderTable.Create(device, finalise.TLAS, entry);
            dispatchDesc.Depth = 1;
            dispatchDesc.Width = camera.ScreenSize.Width;
            dispatchDesc.Height = camera.ScreenSize.Height;
            entry.List.DispatchRays(dispatchDesc);

            var commit = new RaytraceCommit { RenderTarget = rp.RenderTarget, List = entry };
            foreach (var shader in Shaders)
            {
                shader.CommitRaytracing(commit);
            }

            entry.Execute();
        }

        private Vortice.Direct3D12.ID3D12Resource CreateTLAS(RaytracePreparation preparation)
        {
            var instances = preparation.List.DisposeAfterExecution(preparation.List.CreateUploadBuffer(preparation.InstanceDescriptions)).Name("TLAS prep buffer");

            var asDesc = new Vortice.Direct3D12.BuildRaytracingAccelerationStructureInputs
            {
                DescriptorsCount = preparation.InstanceDescriptions.Count,
                Flags = Vortice.Direct3D12.RaytracingAccelerationStructureBuildFlags.None,
                Layout = Vortice.Direct3D12.ElementsLayout.Array,
                Type = Vortice.Direct3D12.RaytracingAccelerationStructureType.TopLevel,
                InstanceDescriptions = instances.GPUVirtualAddress,
            };

            var prebuild = device.GetRaytracingAccelerationStructurePrebuildInfo(asDesc);

            var scratch = preparation.List.DisposeAfterExecution(device.CreateStaticBuffer(prebuild.ScratchDataSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.Common, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name("TLAS scratch"));
            var result = preparation.List.DisposeAfterExecution(device.CreateStaticBuffer(prebuild.ResultDataMaxSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.RaytracingAccelerationStructure, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name("TLAS result"));

            preparation.List.List.BuildRaytracingAccelerationStructure(new Vortice.Direct3D12.BuildRaytracingAccelerationStructureDescription
            {
                DestinationAccelerationStructureData = result.GPUVirtualAddress,
                ScratchAccelerationStructureData = scratch.GPUVirtualAddress,
                Inputs = asDesc,
                SourceAccelerationStructureData = 0,
            });

            preparation.List.List.ResourceBarrierUnorderedAccessView(result);

            return result;
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }
    }
}
