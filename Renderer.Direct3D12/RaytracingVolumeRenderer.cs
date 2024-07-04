using Data.Space;
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
        private readonly DescriptorHeapAccumulator heapAccumulator;

        private readonly Vortice.Direct3D12.ID3D12RootSignature emptyGlobalSignature;
        private readonly Vortice.Direct3D12.ID3D12StateObject state;
        private readonly StateObjectProperties stateObjectProperties;


        private readonly Shaders.Raytrace.Hit.ObjectRadiance objectRadiance;
        private readonly Shaders.Raytrace.Hit.ObjectShadow objectShadow;
        private readonly Shaders.Raytrace.Hit.SphereIntersection sphereIntersection;
        private readonly Shaders.Raytrace.Hit.SphereRadiance sphereRadiance;
        private readonly Shaders.Raytrace.Hit.SphereShadow sphereShadow;
        private readonly Shaders.Raytrace.RayGen.Camera cameraShader;
        private readonly Shaders.Raytrace.RayGen.Filter filterShader;
        private readonly Shaders.Raytrace.Miss.RadianceMiss radianceMiss;
        private readonly Shaders.Raytrace.Miss.ShadowMiss shadowMiss;

        private readonly Shaders.MissShaders missShaderStep;
        private readonly Shaders.CameraRayGen rayGenStep;

        private readonly Shaders.ObjectStep objectStep;

        public RaytracingVolumeRenderer(DescriptorHeapAccumulator heapAccumulator, MeshResourceCache meshResourceCache, MapResourceCache mapResourceCache, Vortice.Direct3D12.ID3D12Device5 device, CommandListPool directListPool, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            this.directListPool = directListPool;
            this.device = device;
            this.heapAccumulator = heapAccumulator;

            objectRadiance = disposeTracker.Track(new Shaders.Raytrace.Hit.ObjectRadiance(device));
            objectShadow = disposeTracker.Track(new Shaders.Raytrace.Hit.ObjectShadow(device));
            sphereIntersection = disposeTracker.Track(new Shaders.Raytrace.Hit.SphereIntersection(device));
            sphereRadiance = disposeTracker.Track(new Shaders.Raytrace.Hit.SphereRadiance(device));
            sphereShadow = disposeTracker.Track(new Shaders.Raytrace.Hit.SphereShadow(device));
            cameraShader = disposeTracker.Track(new Shaders.Raytrace.RayGen.Camera(device));
            radianceMiss = disposeTracker.Track(new Shaders.Raytrace.Miss.RadianceMiss(device));
            shadowMiss = disposeTracker.Track(new Shaders.Raytrace.Miss.ShadowMiss(device));
            filterShader = disposeTracker.Track(new Shaders.Raytrace.RayGen.Filter(device));

            missShaderStep = disposeTracker.Track(new Shaders.MissShaders(mapResourceCache, radianceMiss, shadowMiss));
            rayGenStep = disposeTracker.Track(new Shaders.CameraRayGen(device, screenSize, renderTargetFormat, filterShader, cameraShader));

            objectStep = disposeTracker.Track(new Shaders.ObjectStep(meshResourceCache, mapResourceCache, maxRays, objectRadiance, objectShadow, sphereRadiance, sphereShadow, sphereIntersection));

            emptyGlobalSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1())).Name("Empty global signature");

            var shaderConfigSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingShaderConfig { MaxAttributeSizeInBytes = 8, MaxPayloadSizeInBytes = 32 });

            Vortice.Direct3D12.StateSubObject[] fixedSubobjects = [
                shaderConfigSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(shaderConfigSubobject, RaytracingShaders.Select(s => s.Export).ToArray())),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingPipelineConfig(maxRays))
            ];

            state = disposeTracker.Track(device.CreateStateObject(new Vortice.Direct3D12.StateObjectDescription(Vortice.Direct3D12.StateObjectType.RaytracingPipeline,
                RaytracingShaders.SelectMany(s => s.CreateStateObjects())
                    .Concat(fixedSubobjects)
                    .Concat(Steps.SelectMany(s => s.CreateStateObjects()))
                    .ToArray()))
                .Name("Raytrace state object"));

            stateObjectProperties = disposeTracker.Track(new StateObjectProperties(state));
        }

        private Shaders.ILibrary[] RaytracingShaders => [objectRadiance, objectShadow, sphereIntersection, sphereRadiance, sphereShadow, radianceMiss, shadowMiss, cameraShader];
        private Shaders.IRaytracingPipelineStep[] Steps => [missShaderStep, rayGenStep, objectStep];

        public void Render(RendererParameters rp, Volume volume, Camera camera)
        {
            if (volume == null) return;

            var entry = directListPool.GetCommandList();

            var preparation = new Shaders.RaytracePreparation 
            { 
                Camera = camera, 
                Volume = volume, 
                InstanceDescriptions = new List<Vortice.Direct3D12.RaytracingInstanceDescription>(), 
                List = entry,
                ShaderTable = entry.DisposeAfterExecution(new ShaderBindingTable(stateObjectProperties)),
                HeapAccumulator = heapAccumulator
            };

            foreach (var step in Steps)
            {
                step.PrepareRaytracing(preparation);
            }

            var tlas = CreateTLAS(preparation);

            entry.List.SetComputeRootSignature(emptyGlobalSignature);
            entry.List.SetPipelineState1(state);
            entry.List.SetDescriptorHeaps(heapAccumulator.GetHeaps());

            var dispatchDesc = preparation.ShaderTable.Create(device, tlas, entry);
            dispatchDesc.Depth = 1;
            dispatchDesc.Width = camera.ScreenSize.Width;
            dispatchDesc.Height = camera.ScreenSize.Height;
            entry.List.DispatchRays(dispatchDesc);

            var commit = new Shaders.RaytraceCommit { RenderTarget = rp.RenderTarget, List = entry };
            foreach (var step in Steps)
            {
                step.CommitRaytracing(commit);
            }

            entry.Execute();
        }

        private Vortice.Direct3D12.ID3D12Resource CreateTLAS(Shaders.RaytracePreparation preparation)
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
