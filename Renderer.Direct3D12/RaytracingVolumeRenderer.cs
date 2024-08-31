using Data.Space;
using Renderer.Direct3D12.Shaders;
using Simulation;
using Simulation.Physics;
using Util;
using static Renderer.Direct3D12.Shaders.ScreenSizeRaytraceResources;

namespace Renderer.Direct3D12
{
    internal class RaytracingVolumeRenderer : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private const int maxRays = 3;

        private readonly CommandListPool directListPool;
        private readonly Vortice.Direct3D12.ID3D12Device5 device;
        private readonly DescriptorHeapAccumulator heapAccumulator;
        private readonly MapResourceCache mapResourceCache;

        private readonly Vortice.Direct3D12.ID3D12RootSignature emptyGlobalSignature;
        private readonly Vortice.Direct3D12.ID3D12StateObject state;
        private readonly StateObjectProperties stateObjectProperties;


        private readonly Shaders.Raytrace.Hit.ObjectRadiance objectRadiance;
        private readonly Shaders.Raytrace.Hit.SphereIntersection sphereIntersection;
        private readonly Shaders.Raytrace.Hit.SphereRadiance sphereRadiance;
        private readonly Shaders.Raytrace.RayGen.Camera cameraShader;
        private readonly Shaders.Raytrace.Filtering.Filter filterShader;
        private readonly Shaders.Raytrace.Miss.RadianceMiss radianceMiss;
        private readonly Shaders.Raytrace.Filtering.Atrous atrousShader;

        private readonly Shaders.ObjectStep objectStep;
        private readonly ScreenSizeDependent<ScreenSizeRaytraceResources> screenRaytraceResources;

        private int frameCount = 0;

        public RaytracingVolumeRenderer(DescriptorHeapAccumulator heapAccumulator, MeshResourceCache meshResourceCache, MapResourceCache mapResourceCache, Vortice.Direct3D12.ID3D12Device5 device, CommandListPool directListPool, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            this.directListPool = directListPool;
            this.device = device;
            this.heapAccumulator = heapAccumulator;
            this.mapResourceCache = mapResourceCache;

            objectRadiance = disposeTracker.Track(new Shaders.Raytrace.Hit.ObjectRadiance(device));
            sphereIntersection = disposeTracker.Track(new Shaders.Raytrace.Hit.SphereIntersection(device));
            sphereRadiance = disposeTracker.Track(new Shaders.Raytrace.Hit.SphereRadiance(device));
            cameraShader = disposeTracker.Track(new Shaders.Raytrace.RayGen.Camera(device));
            radianceMiss = disposeTracker.Track(new Shaders.Raytrace.Miss.RadianceMiss(device));
            filterShader = disposeTracker.Track(new Shaders.Raytrace.Filtering.Filter(device));
            atrousShader = disposeTracker.Track(new Shaders.Raytrace.Filtering.Atrous(device));

            objectStep = disposeTracker.Track(new Shaders.ObjectStep(meshResourceCache, maxRays, objectRadiance, sphereRadiance, sphereIntersection));

            emptyGlobalSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.ConstantBufferViewShaderResourceViewUnorderedAccessViewHeapDirectlyIndexed))).Name("Empty global signature");

            var shaderConfigSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingShaderConfig { MaxAttributeSizeInBytes = 8, MaxPayloadSizeInBytes = 4 });
            var globalSignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.GlobalRootSignature(emptyGlobalSignature));

            Vortice.Direct3D12.StateSubObject[] fixedSubobjects = [
                globalSignatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(globalSignatureSubobject, RaytracingShaders.Select(s => s.Export).ToArray())),
                shaderConfigSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(shaderConfigSubobject, RaytracingShaders.Select(s => s.Export).ToArray())),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingPipelineConfig(maxRays))
            ];

            state = disposeTracker.Track(device.CreateStateObject(new Vortice.Direct3D12.StateObjectDescription(Vortice.Direct3D12.StateObjectType.RaytracingPipeline,
                RaytracingShaders.SelectMany(s => s.CreateStateObjects())
                    .Concat(fixedSubobjects)
                    .Concat(objectStep.CreateStateObjects())
                    .ToArray()))
                .Name("Raytrace state object"));

            stateObjectProperties = disposeTracker.Track(new StateObjectProperties(state));
            screenRaytraceResources = disposeTracker.Track(new ScreenSizeDependent<ScreenSizeRaytraceResources>(screenSize, size => new ScreenSizeRaytraceResources(device, size, renderTargetFormat)));
        }

        private Shaders.ILibrary[] RaytracingShaders => [objectRadiance, sphereIntersection, sphereRadiance, radianceMiss, cameraShader];

        public void Render(RendererParameters rp, Volume volume, Camera camera)
        {
            if (volume == null) return;

            var entry = directListPool.GetCommandList();
            var frameData = screenRaytraceResources.GetFor(camera.ScreenSize);
            var shaderTable = entry.DisposeAfterExecution(new ShaderBindingTable(stateObjectProperties));

            using (var dataLease = frameData.ResourcePool.LeaseResource(frameData.FrameDataKey, $"Frame {frameCount} data buffer"))
            using (var atrousLease = frameData.ResourcePool.LeaseResource(frameData.AtrousTextureKey, $"Frame {frameCount} atrous buffer"))
            {
                var outputTextureLease = frameData.ResourcePool.LeaseResource(frameData.FrameTextureKey, $"Frame {frameCount} raytrace output texture");

                AddRayGen(shaderTable, camera);
                AddMissShader(volume, shaderTable, outputTextureLease, atrousLease, dataLease, entry);
                var instanceDescriptions = objectStep.PrepareRaytracing(volume, heapAccumulator, entry, shaderTable, outputTextureLease, atrousLease, dataLease).ToArray();

                var tlas = CreateTLAS(instanceDescriptions, entry);

                entry.List.SetDescriptorHeaps(heapAccumulator.GetHeaps());
                entry.List.SetComputeRootSignature(emptyGlobalSignature);
                entry.List.SetPipelineState1(state);

                var dispatchDesc = shaderTable.Create(device, tlas, entry);
                dispatchDesc.Depth = 1;
                dispatchDesc.Width = camera.ScreenSize.Width;
                dispatchDesc.Height = camera.ScreenSize.Height;
                entry.List.DispatchRays(dispatchDesc);

                Filter(entry, camera, heapAccumulator, frameData, outputTextureLease, atrousLease, dataLease, rp.RenderTarget);
            }

            entry.Execute();

            frameCount++;
        }

        private void Filter(PooledCommandList entry, Camera camera, DescriptorHeapAccumulator heapAccumulator, ScreenSizeRaytraceResources resources, ResourcePool.ResourceLifetime<IlluminanceTextureKey> input, ResourcePool.ResourceLifetime<AtrousDataTextureKey> atrous, ResourcePool.ResourceLifetime<GBufferKey> inputData, Vortice.Direct3D12.ID3D12Resource renderTarget)
        {
            entry.List.ResourceBarrierUnorderedAccessView(input.Resource);

            entry.List.SetPipelineState(atrousShader.PipelineState);
            entry.List.SetComputeRootSignature(atrousShader.RootSignature);

            ResourcePool.ResourceLifetime<IlluminanceTextureKey> output;

            for (int i = 0; i < 5; i++)
            {
                output = resources.ResourcePool.LeaseResource(resources.FrameTextureKey, $"{frameCount} a-trous output buffer {i}");

                var stepFactor = (float)Math.Pow(2, -i);

                entry.List.SetComputeRoot32BitConstants(0, [new Shaders.Data.AtrousRootParameters
                {
                    ImageHeight = (uint)camera.ScreenSize.Height,
                    ImageWidth = (uint)camera.ScreenSize.Width,
                    StepWidth = i,
                    CPhi = stepFactor * 1000000.0f,
                    NPhi = 0.01f,
                    OutputTextureIndex = heapAccumulator.AddUAV(output.Resource, output.Key.UAV),
                    InputDataIndex = heapAccumulator.AddUAV(atrous.Resource, atrous.Key.UAV),
                    InputTextureIndex = heapAccumulator.AddUAV(input.Resource, input.Key.UAV),
                }]);
                entry.List.Dispatch((int)Math.Ceiling(camera.ScreenSize.Width / (float)32), (int)Math.Ceiling(camera.ScreenSize.Height / (float)32), 1);

                entry.List.ResourceBarrierUnorderedAccessView(output.Resource);

                input.Dispose();
                input = output;
            }

            using (var outputTextureLease = resources.ResourcePool.LeaseResource(resources.FrameTextureKey, $"{frameCount} output texture"))
            {
                entry.List.ResourceBarrier([
                    new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(outputTextureLease.Resource, Vortice.Direct3D12.ResourceStates.CopySource, Vortice.Direct3D12.ResourceStates.UnorderedAccess))
                ]);

                entry.List.SetPipelineState(filterShader.PipelineState);
                entry.List.SetComputeRootSignature(filterShader.RootSignature);
                entry.List.SetComputeRoot32BitConstants(0, [new Shaders.Data.FilterParameters
                {
                    ImageHeight = (uint)camera.ScreenSize.Height,
                    ImageWidth = (uint)camera.ScreenSize.Width,
                    OutputTextureIndex = heapAccumulator.AddUAV(outputTextureLease.Resource, outputTextureLease.Key.UAV),
                    InputDataIndex = heapAccumulator.AddUAV(inputData.Resource, inputData.Key.UAV),
                    InputTextureIndex = heapAccumulator.AddUAV(input.Resource, input.Key.UAV),
                }]);
                entry.List.Dispatch((int)Math.Ceiling(camera.ScreenSize.Width / (float)32), (int)Math.Ceiling(camera.ScreenSize.Height / (float)32), 1);

                entry.List.ResourceBarrier([
                    new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(outputTextureLease.Resource, Vortice.Direct3D12.ResourceStates.UnorderedAccess, Vortice.Direct3D12.ResourceStates.CopySource)),
                    new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(renderTarget, Vortice.Direct3D12.ResourceStates.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest))
                ]);

                entry.List.CopyResource(renderTarget, outputTextureLease.Resource);
                entry.List.ResourceBarrierTransition(renderTarget, Vortice.Direct3D12.ResourceStates.CopyDest, Vortice.Direct3D12.ResourceStates.RenderTarget);
            }

            input.Dispose();
        }

        private void AddRayGen(ShaderBindingTable shaderTable, Camera camera)
        {
            var frustum = Frustum.FromScreen(new ScreenRectangle { Start = new ScreenPosition(0, 0), End = new ScreenPosition(camera.ScreenSize.Width, camera.ScreenSize.Height) }, camera.ScreenSize, camera.InvViewProjection);

            shaderTable.AddRayGeneration(cameraShader.Export, tlas => new Shaders.Data.CameraMatrices
            {
                WorldBottomLeft = frustum.Points[0],
                WorldTopLeft = frustum.Points[1],
                WorldTopRight = frustum.Points[2],
                Origin = camera.Position,
                SceneBVHIndex = heapAccumulator.AddRaytracingStructure(tlas),
            }.GetBytes());
        }

        private void AddMissShader(Volume volume, ShaderBindingTable shaderTable, ResourcePool.ResourceLifetime<IlluminanceTextureKey> illuminanceTexture, ResourcePool.ResourceLifetime<AtrousDataTextureKey> atrous, ResourcePool.ResourceLifetime<GBufferKey> data, PooledCommandList entry)
        {
            var mapData = mapResourceCache.Get(volume.Map, entry);

            var parameters = new Shaders.Data.StarfieldParameters
            {
                DataIndex = heapAccumulator.AddUAV(data.Resource, data.Key.UAV),
                IlluminanceTextureIndex = heapAccumulator.AddUAV(illuminanceTexture.Resource, illuminanceTexture.Key.UAV),
                NoiseScale = volume.Map.StarfieldNoiseScale,
                NoiseCutoff = volume.Map.StarfieldNoiseCutoff,
                TemperatureScale = volume.Map.StarfieldTemperatureScale,
                StarCategories = (uint)volume.Map.StarCategories.Length,
                Seed = mapData.Seed,
                AmbientLight = volume.Map.AmbientLightLevel,
                CategoryIndex = heapAccumulator.AddStructuredBuffer(mapData.Categories),
                AtrousDataTextureIndex = heapAccumulator.AddUAV(atrous.Resource, atrous.Key.UAV),
            };

            shaderTable.AddMiss(radianceMiss.Export, tlas => parameters.GetBytes());
        }

        private Vortice.Direct3D12.ID3D12Resource CreateTLAS(Vortice.Direct3D12.RaytracingInstanceDescription[] descriptions, PooledCommandList list)
        {
            var instances = list.DisposeAfterExecution(list.CreateUploadBuffer(descriptions)).Name("TLAS prep buffer");

            var asDesc = new Vortice.Direct3D12.BuildRaytracingAccelerationStructureInputs
            {
                DescriptorsCount = descriptions.Length,
                Flags = Vortice.Direct3D12.RaytracingAccelerationStructureBuildFlags.None,
                Layout = Vortice.Direct3D12.ElementsLayout.Array,
                Type = Vortice.Direct3D12.RaytracingAccelerationStructureType.TopLevel,
                InstanceDescriptions = instances.Buffer.GPUVirtualAddress,
            };

            var prebuild = device.GetRaytracingAccelerationStructurePrebuildInfo(asDesc);

            var scratch = list.DisposeAfterExecution(device.CreateStaticBuffer(prebuild.ScratchDataSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.Common, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name("TLAS scratch"));
            var result = list.DisposeAfterExecution(device.CreateStaticBuffer(prebuild.ResultDataMaxSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.RaytracingAccelerationStructure, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name("TLAS result"));

            list.List.BuildRaytracingAccelerationStructure(new Vortice.Direct3D12.BuildRaytracingAccelerationStructureDescription
            {
                DestinationAccelerationStructureData = result.GPUVirtualAddress,
                ScratchAccelerationStructureData = scratch.GPUVirtualAddress,
                Inputs = asDesc,
                SourceAccelerationStructureData = 0,
            });

            list.List.ResourceBarrierUnorderedAccessView(result);

            return result;
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }
    }
}
