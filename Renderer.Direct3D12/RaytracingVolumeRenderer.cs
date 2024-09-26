using Data.Space;
using Simulation;
using Simulation.Physics;
using Util;
using static Renderer.Direct3D12.ScreenSizeRaytraceResources;

namespace Renderer.Direct3D12
{
    internal class RaytracingVolumeRenderer : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private const int maxRays = 3;

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
        private readonly Shaders.Raytrace.Filtering.Variance varianceShader;

        private readonly Shaders.ObjectStep objectStep;
        private readonly ScreenSizeDependent<ScreenSizeRaytraceResources> screenRaytraceResources;

        private int frameCount = 0;

        public RaytracingVolumeRenderer(PermanentResources permanentResources, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            objectRadiance = disposeTracker.Track(new Shaders.Raytrace.Hit.ObjectRadiance(permanentResources.Device));
            sphereIntersection = disposeTracker.Track(new Shaders.Raytrace.Hit.SphereIntersection(permanentResources.Device));
            sphereRadiance = disposeTracker.Track(new Shaders.Raytrace.Hit.SphereRadiance(permanentResources.Device));
            cameraShader = disposeTracker.Track(new Shaders.Raytrace.RayGen.Camera(permanentResources.Device));
            radianceMiss = disposeTracker.Track(new Shaders.Raytrace.Miss.RadianceMiss(permanentResources.Device));
            filterShader = disposeTracker.Track(new Shaders.Raytrace.Filtering.Filter(permanentResources.Device));
            atrousShader = disposeTracker.Track(new Shaders.Raytrace.Filtering.Atrous(permanentResources.Device));
            varianceShader = disposeTracker.Track(new Shaders.Raytrace.Filtering.Variance(permanentResources.Device));

            objectStep = disposeTracker.Track(new Shaders.ObjectStep(maxRays, objectRadiance, sphereRadiance, sphereIntersection));

            emptyGlobalSignature = disposeTracker.Track(permanentResources.Device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.ConstantBufferViewShaderResourceViewUnorderedAccessViewHeapDirectlyIndexed))).Name("Empty global signature");

            var shaderConfigSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingShaderConfig { MaxAttributeSizeInBytes = 8, MaxPayloadSizeInBytes = 4 });
            var globalSignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.GlobalRootSignature(emptyGlobalSignature));

            Vortice.Direct3D12.StateSubObject[] fixedSubobjects = [
                globalSignatureSubobject,
                shaderConfigSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingPipelineConfig(maxRays))
            ];

            state = disposeTracker.Track(permanentResources.Device.CreateStateObject(new Vortice.Direct3D12.StateObjectDescription(Vortice.Direct3D12.StateObjectType.RaytracingPipeline,
                RaytracingShaders.SelectMany(s => s.CreateStateObjects())
                    .Concat(fixedSubobjects)
                    .Concat(objectStep.CreateStateObjects())
                    .ToArray()))
                .Name("Raytrace state object"));

            stateObjectProperties = disposeTracker.Track(new StateObjectProperties(state));
            screenRaytraceResources = disposeTracker.Track(new ScreenSizeDependent<ScreenSizeRaytraceResources>(screenSize, size => new ScreenSizeRaytraceResources(permanentResources.Device, size, renderTargetFormat)));
        }

        private Shaders.ILibrary[] RaytracingShaders => [objectRadiance, sphereIntersection, sphereRadiance, radianceMiss, cameraShader];

        public void Render(FrameResources resources, Vortice.Direct3D12.ID3D12Resource renderTarget, Volume volume, Camera camera)
        {
            if (volume == null) return;

            var frameData = screenRaytraceResources.GetFor(camera.ScreenSize);
            var shaderTable = new ShaderBindingTable(stateObjectProperties);
            var commandList = resources.Permanent.CommandList;

            using (var dataLease = frameData.ResourcePool.LeaseResource(frameData.FrameDataKey, $"Frame {frameCount} data buffer"))
            using (var atrousLease = frameData.ResourcePool.LeaseResource(frameData.AtrousTextureKey, $"Frame {frameCount} atrous buffer"))
            {
                var outputTextureLease = frameData.ResourcePool.LeaseResource(frameData.FrameTextureKey, $"Frame {frameCount} raytrace output texture");
                var tlasReservation = resources.HeapAccumulator.ReserveRaytracingStructure();
                
                AddRayGen(shaderTable, camera, tlasReservation.Offset);
                AddMissShader(volume, shaderTable, outputTextureLease, atrousLease, dataLease, resources);
                var instanceDescriptions = objectStep.PrepareRaytracing(volume, tlasReservation.Offset, resources, shaderTable, outputTextureLease, atrousLease, dataLease, frameData.PreviousFrameIlluminance).ToArray();
                
                tlasReservation.Commit(CreateTLAS(resources, instanceDescriptions));
                
                commandList.SetDescriptorHeaps(resources.HeapAccumulator.GetHeaps());
                commandList.SetComputeRootSignature(emptyGlobalSignature);
                commandList.SetPipelineState1(state);
                
                var dispatchDesc = shaderTable.Create(resources);
                dispatchDesc.Depth = 1;
                dispatchDesc.Width = camera.ScreenSize.Width;
                dispatchDesc.Height = camera.ScreenSize.Height;
                commandList.DispatchRays(dispatchDesc);
                
                if (frameData.PreviousFrameIlluminance != null)
                {
                    commandList.Barrier(new Vortice.Direct3D12.BarrierGroup([new Vortice.Direct3D12.TextureBarrier {
                        Resource = frameData.PreviousFrameIlluminance.Resource,
                        SyncBefore = Vortice.Direct3D12.BarrierSync.Raytracing,
                        SyncAfter = Vortice.Direct3D12.BarrierSync.All,
                        AccessBefore = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                        AccessAfter = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                        LayoutBefore = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                        LayoutAfter = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                    }]));
                    frameData.PreviousFrameIlluminance.Dispose();
                }
                
                Filter(camera, resources, frameData, outputTextureLease, atrousLease, dataLease, renderTarget);

                outputTextureLease.Dispose();
            }

            frameCount++;
        }

        private void Filter(Camera camera, FrameResources frameResources, ScreenSizeRaytraceResources resources, ResourcePool.ResourceLifetime<IlluminanceTextureKey> input, ResourcePool.ResourceLifetime<AtrousDataTextureKey> atrous, ResourcePool.ResourceLifetime<GBufferKey> inputData, Vortice.Direct3D12.ID3D12Resource renderTarget)
        {
            var commandList = frameResources.Permanent.CommandList;
            var heapAccumulator = frameResources.HeapAccumulator;

            commandList.Barrier(new Vortice.Direct3D12.BarrierGroup([new Vortice.Direct3D12.TextureBarrier {
                Resource = input.Resource,
                SyncBefore = Vortice.Direct3D12.BarrierSync.Raytracing,
                SyncAfter = Vortice.Direct3D12.BarrierSync.ComputeShading,
                AccessBefore = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                AccessAfter = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                LayoutBefore = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                LayoutAfter = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
            }]));
            commandList.Barrier(new Vortice.Direct3D12.BarrierGroup([new Vortice.Direct3D12.BufferBarrier {
                Resource = inputData.Resource,
                Offset = 0,
                Size = inputData.Resource.Description.Width,
                SyncBefore = Vortice.Direct3D12.BarrierSync.Raytracing,
                SyncAfter = Vortice.Direct3D12.BarrierSync.ComputeShading,
                AccessBefore = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                AccessAfter = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
            }]));

            var variance = resources.ResourcePool.LeaseResource(resources.VarianceTextureKey, $"{frameCount} variance starter texture");

            commandList.SetPipelineState(varianceShader.PipelineState);
            commandList.SetComputeRootSignature(varianceShader.RootSignature);
            commandList.SetComputeRoot32BitConstants(0, [new Shaders.Data.VarianceRootParameters
            {
                 ImageHeight = camera.ScreenSize.Height,
                 ImageWidth = camera.ScreenSize.Width,
                 StdDevTextureIndex = heapAccumulator.AddUAV(resources.ImageStdDevTexture.Resource, resources.ImageStdDevTexture.Key.UAV),
                 MeanTextureIndex = heapAccumulator.AddUAV(resources.ImageMeanTexture.Resource, resources.ImageMeanTexture.Key.UAV),
                 IlluminanceTextureIndex = heapAccumulator.AddUAV(input.Resource, input.Key.UAV),
                 AtrousDataIndex = heapAccumulator.AddUAV(atrous.Resource, atrous.Key.UAV),
                 VarianceTextureIndex = heapAccumulator.AddUAV(variance.Resource, variance.Key.UAV)
            }]);
            commandList.Dispatch((uint)Math.Ceiling(camera.ScreenSize.Width / (float)32), (uint)Math.Ceiling(camera.ScreenSize.Height / (float)32), 1);

            commandList.Barrier(new Vortice.Direct3D12.BarrierGroup([new Vortice.Direct3D12.TextureBarrier {
                Resource = variance.Resource,
                SyncBefore = Vortice.Direct3D12.BarrierSync.ComputeShading,
                SyncAfter = Vortice.Direct3D12.BarrierSync.ComputeShading,
                AccessBefore = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                AccessAfter = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                LayoutBefore = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                LayoutAfter = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
            }]));

            commandList.SetPipelineState(atrousShader.PipelineState);
            commandList.SetComputeRootSignature(atrousShader.RootSignature);

            ResourcePool.ResourceLifetime<IlluminanceTextureKey> output;
            ResourcePool.ResourceLifetime<VarianceDataTextureKey> outputVariance;

            for (int i = 0; i < 5; i++)
            {
                output = resources.ResourcePool.LeaseResource(resources.FrameTextureKey, $"{frameCount} a-trous illuminance output buffer {i + 1}");
                outputVariance = resources.ResourcePool.LeaseResource(resources.VarianceTextureKey, $"{frameCount} a-trous variance output buffer {i + 1}");

                commandList.SetComputeRoot32BitConstants(0, [new Shaders.Data.AtrousRootParameters
                {
                    ImageHeight = camera.ScreenSize.Height,
                    ImageWidth = camera.ScreenSize.Width,
                    StepWidth = i + 1,
                    OutputIlluminanceTextureIndex = heapAccumulator.AddUAV(output.Resource, output.Key.UAV),
                    InputDataIndex = heapAccumulator.AddUAV(atrous.Resource, atrous.Key.UAV),
                    InputIlluminanceTextureIndex = heapAccumulator.AddUAV(input.Resource, input.Key.UAV),
                    InputVarianceTextureIndex = heapAccumulator.AddUAV(variance.Resource, variance.Key.UAV),
                    OutputVarianceTextureIndex = heapAccumulator.AddUAV(outputVariance.Resource, outputVariance.Key.UAV),
                }]);
                commandList.Dispatch((uint)Math.Ceiling(camera.ScreenSize.Width / (float)32), (uint)Math.Ceiling(camera.ScreenSize.Height / (float)32), 1);
                
                commandList.Barrier(new Vortice.Direct3D12.BarrierGroup([
                    new Vortice.Direct3D12.TextureBarrier {
                        Resource = output.Resource,
                        SyncBefore = Vortice.Direct3D12.BarrierSync.ComputeShading,
                        SyncAfter = Vortice.Direct3D12.BarrierSync.ComputeShading,
                        AccessBefore = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                        AccessAfter = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                        LayoutBefore = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                        LayoutAfter = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                    },
                    new Vortice.Direct3D12.TextureBarrier {
                        Resource = outputVariance.Resource,
                        SyncBefore = Vortice.Direct3D12.BarrierSync.ComputeShading,
                        SyncAfter = Vortice.Direct3D12.BarrierSync.ComputeShading,
                        AccessBefore = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                        AccessAfter = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                        LayoutBefore = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                        LayoutAfter = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                    }
                ]));

                if (i == 2)
                {
                    // Keep the input for the next frame
                    resources.PreviousFrameIlluminance = input;
                } 
                else
                {
                    input.Dispose();
                }
                variance.Dispose();

                variance = outputVariance;
                input = output;
            }

            using (var outputTextureLease = resources.ResourcePool.LeaseResource(resources.FrameTextureKey, $"{frameCount} output texture"))
            {
                commandList.Barrier(new Vortice.Direct3D12.BarrierGroup([new Vortice.Direct3D12.TextureBarrier {
                    Resource = outputTextureLease.Resource,
                    SyncBefore = Vortice.Direct3D12.BarrierSync.ComputeShading,
                    SyncAfter = Vortice.Direct3D12.BarrierSync.ComputeShading,
                    AccessBefore = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                    AccessAfter = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                    LayoutBefore = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                    LayoutAfter = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                }]));

                commandList.SetPipelineState(filterShader.PipelineState);
                commandList.SetComputeRootSignature(filterShader.RootSignature);
                commandList.SetComputeRoot32BitConstants(0, [new Shaders.Data.FilterParameters
                {
                    ImageHeight = (uint)camera.ScreenSize.Height,
                    ImageWidth = (uint)camera.ScreenSize.Width,
                    OutputTextureIndex = heapAccumulator.AddUAV(outputTextureLease.Resource, outputTextureLease.Key.UAV),
                    InputDataIndex = heapAccumulator.AddUAV(inputData.Resource, inputData.Key.UAV),
                    InputTextureIndex = heapAccumulator.AddUAV(input.Resource, input.Key.UAV),
                }]);
                commandList.Dispatch((uint)Math.Ceiling(camera.ScreenSize.Width / (float)32), (uint)Math.Ceiling(camera.ScreenSize.Height / (float)32), 1);

                commandList.Barrier(new Vortice.Direct3D12.BarrierGroup([
                    new Vortice.Direct3D12.TextureBarrier {
                        Resource = outputTextureLease.Resource,
                        SyncBefore = Vortice.Direct3D12.BarrierSync.ComputeShading,
                        SyncAfter = Vortice.Direct3D12.BarrierSync.Copy,
                        AccessBefore = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                        AccessAfter = Vortice.Direct3D12.BarrierAccess.CopySource,
                        LayoutBefore = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                        LayoutAfter = Vortice.Direct3D12.BarrierLayout.CopySource,
                    }
                ]));

                commandList.CopyResource(renderTarget, outputTextureLease.Resource);

                commandList.Barrier(new Vortice.Direct3D12.BarrierGroup([
                    new Vortice.Direct3D12.TextureBarrier {
                        Resource = outputTextureLease.Resource,
                        SyncBefore = Vortice.Direct3D12.BarrierSync.Copy,
                        SyncAfter = Vortice.Direct3D12.BarrierSync.ComputeShading,
                        AccessBefore = Vortice.Direct3D12.BarrierAccess.CopySource,
                        AccessAfter = Vortice.Direct3D12.BarrierAccess.UnorderedAccess,
                        LayoutBefore = Vortice.Direct3D12.BarrierLayout.CopySource,
                        LayoutAfter = Vortice.Direct3D12.BarrierLayout.UnorderedAccess,
                    }
                ]));
            }

            variance.Dispose();
            input.Dispose();
        }

        private void AddRayGen(ShaderBindingTable shaderTable, Camera camera, uint tlas)
        {
            var frustum = Frustum.FromScreen(new ScreenRectangle { Start = new ScreenPosition(0, 0), End = new ScreenPosition((int)camera.ScreenSize.Width, (int)camera.ScreenSize.Height) }, camera.ScreenSize, camera.InvViewProjection);

            shaderTable.AddRayGeneration(cameraShader.Export, new Shaders.Data.CameraMatrices
            {
                WorldBottomLeft = frustum.Points[0],
                WorldTopLeft = frustum.Points[1],
                WorldTopRight = frustum.Points[2],
                Origin = camera.Position,
                SceneBVHIndex = tlas,
            }.GetBytes());
        }

        private void AddMissShader(Volume volume, ShaderBindingTable shaderTable, ResourcePool.ResourceLifetime<IlluminanceTextureKey> illuminanceTexture, ResourcePool.ResourceLifetime<AtrousDataTextureKey> atrous, ResourcePool.ResourceLifetime<GBufferKey> data, FrameResources resources)
        {
            var mapData = resources.Permanent.MapResourceCache.Get(volume.Map, resources);

            var parameters = new Shaders.Data.StarfieldParameters
            {
                DataIndex = resources.HeapAccumulator.AddUAV(data.Resource, data.Key.UAV),
                IlluminanceTextureIndex = resources.HeapAccumulator.AddUAV(illuminanceTexture.Resource, illuminanceTexture.Key.UAV),
                NoiseScale = volume.Map.StarfieldNoiseScale,
                NoiseCutoff = volume.Map.StarfieldNoiseCutoff,
                TemperatureScale = volume.Map.StarfieldTemperatureScale,
                StarCategories = (uint)volume.Map.StarCategories.Length,
                Seed = mapData.Seed,
                AmbientLight = volume.Map.AmbientLightLevel,
                CategoryIndex = resources.HeapAccumulator.AddStructuredBuffer(mapData.Categories),
                AtrousDataTextureIndex = resources.HeapAccumulator.AddUAV(atrous.Resource, atrous.Key.UAV),
            };

            shaderTable.AddMiss(radianceMiss.Export, parameters.GetBytes());
        }

        private BufferView CreateTLAS(FrameResources frameResources, Vortice.Direct3D12.RaytracingInstanceDescription[] descriptions)
        {
            return frameResources.BuildAS(frameResources.FrameTLASPool, new Vortice.Direct3D12.BuildRaytracingAccelerationStructureInputs
            {
                DescriptorsCount = (uint)descriptions.Length,
                Flags = Vortice.Direct3D12.RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
                Layout = Vortice.Direct3D12.ElementsLayout.Array,
                Type = Vortice.Direct3D12.RaytracingAccelerationStructureType.TopLevel,
                InstanceDescriptions = frameResources.TransferToUnorderedAccess(descriptions).GPUVirtualAddress,
            });
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }
    }
}
