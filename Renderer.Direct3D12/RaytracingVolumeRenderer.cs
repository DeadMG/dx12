using Data.Space;
using Simulation;
using Simulation.Physics;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    internal class RaytracingVolumeRenderer : IDisposable
    {
        private readonly VertexCalculator vertexCalculator = new VertexCalculator();
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly MeshResourceCache meshResourceCache;
        private readonly CommandListPool directListPool;
        private readonly Vortice.Direct3D12.ID3D12Device5 device;
        private readonly Vortice.DXGI.Format renderTargetFormat;

        private readonly Vortice.Direct3D12.ID3D12DescriptorHeap srvUavHeap;
        private readonly Vortice.Direct3D12.ID3D12RootSignature rayGenSignature;
        private readonly Vortice.Direct3D12.ID3D12RootSignature hitSignature;
        private readonly Vortice.Direct3D12.ID3D12RootSignature emptyGlobalSignature;
        private readonly Vortice.Direct3D12.ID3D12RootSignature sunSignature;
        private readonly Vortice.Direct3D12.ID3D12RootSignature emptySignature;
        private readonly Vortice.Direct3D12.ID3D12StateObject state;
        private readonly StateObjectProperties stateObjectProperties;
        private readonly IcosphereMesh sunMesh;

        private RaytracingScreenResources screenResources;

        public RaytracingVolumeRenderer(MeshResourceCache meshResourceCache, Vortice.Direct3D12.ID3D12Device5 device, CommandListPool directListPool, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            this.meshResourceCache = meshResourceCache;
            this.directListPool = directListPool;
            this.device = device;
            this.renderTargetFormat = renderTargetFormat;

            var tableParameter = new Vortice.Direct3D12.RootParameter1(
                new Vortice.Direct3D12.RootDescriptorTable1(
                    new Vortice.Direct3D12.DescriptorRange1(Vortice.Direct3D12.DescriptorRangeType.UnorderedAccessView, 1, 0),
                    new Vortice.Direct3D12.DescriptorRange1(Vortice.Direct3D12.DescriptorRangeType.ShaderResourceView, 1, 0),
                    new Vortice.Direct3D12.DescriptorRange1(Vortice.Direct3D12.DescriptorRangeType.ConstantBufferView, 1, 0)),
                Vortice.Direct3D12.ShaderVisibility.All);

            rayGenSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [tableParameter])).Name("Ray gen root signature"));

            var verticesParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 0 }, Vortice.Direct3D12.ShaderVisibility.All);
            var indicesParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 1 }, Vortice.Direct3D12.ShaderVisibility.All);
            var lightParameter = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<Light>() / 4), Vortice.Direct3D12.ShaderVisibility.All);
            var sunParameter = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<Sun>() / 4), Vortice.Direct3D12.ShaderVisibility.All);

            sunSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [sunParameter])).Name("Sun signature"));
            hitSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [verticesParameter, indicesParameter, lightParameter])).Name("Hit signature"));
            emptySignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature)).Name("Empty local signature"));
            emptyGlobalSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1())).Name("Empty global signature");

            var most = Shader.LoadDxil("raytrace.hlsl", "lib_6_3");
            var hit = Shader.LoadDxil("hit.hlsl", "lib_6_3");
            var sun = Shader.LoadDxil("sunhit.hlsl", "lib_6_3");

            var shaderConfigSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingShaderConfig { MaxAttributeSizeInBytes = 8, MaxPayloadSizeInBytes = 16 });
            var rayGenSignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(rayGenSignature));
            var hitSignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(hitSignature));
            var emptySignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(emptySignature));
            var sunSignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(sunSignature));

            state = disposeTracker.Track(device.CreateStateObject(new Vortice.Direct3D12.StateObjectDescription(Vortice.Direct3D12.StateObjectType.RaytracingPipeline,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(most,
                     new Vortice.Direct3D12.ExportDescription("RayGen"),
                     new Vortice.Direct3D12.ExportDescription("Miss"))),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(hit,
                     new Vortice.Direct3D12.ExportDescription("ClosestObjectHit"))),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(sun,
                     new Vortice.Direct3D12.ExportDescription("ClosestSunHit"))),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "ObjectHitGroup",
                    ClosestHitShaderImport = "ClosestObjectHit",
                }),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "SunHitGroup",
                    ClosestHitShaderImport = "ClosestSunHit",
                }),
                shaderConfigSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(shaderConfigSubobject, "ClosestSunHit", "ClosestObjectHit", "RayGen", "Miss")),
                rayGenSignatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(rayGenSignatureSubobject, "RayGen")),
                hitSignatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(hitSignatureSubobject, "ObjectHitGroup")),
                sunSignatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(sunSignatureSubobject, "SunHitGroup")),
                emptySignatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(emptySignatureSubobject, "Miss")),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingPipelineConfig(1))))
                .Name("Raytrace state object"));

            stateObjectProperties = disposeTracker.Track(new StateObjectProperties(state));

            srvUavHeap = disposeTracker.Track(device.CreateDescriptorHeap(new Vortice.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = 3,
                Flags = Vortice.Direct3D12.DescriptorHeapFlags.ShaderVisible,
                NodeMask = 0,
                Type = Vortice.Direct3D12.DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            }).Name("Raytrace SRV/UAV heap"));

            screenResources = new RaytracingScreenResources(device, stateObjectProperties, srvUavHeap, screenSize,  renderTargetFormat);

            sunMesh = new IcosphereGenerator().Generate(3);
        }

        public void OnResize(ScreenSize newSize)
        {
            screenResources.Dispose();
            screenResources = new RaytracingScreenResources(device, stateObjectProperties, srvUavHeap, newSize, renderTargetFormat);
        }

        public void Render(RendererParameters rp, Volume volume, Camera camera)
        {
            if (volume == null) return;

            var entry = directListPool.GetCommandList();

            var byBlueprint = volume.Units.ToLookup(u => u.Blueprint)
                .Select(unitGroup => new UnitsByBlueprint
                {
                    Blueprint = unitGroup.Key,
                    Units = unitGroup.ToArray(),
                    Data = meshResourceCache.Load(unitGroup.Key.Mesh.Id, unitGroup.Key.Name, () => vertexCalculator.CalculateVertices(unitGroup.Key.Mesh), unitGroup.Key.Mesh.Indices, entry)
                })
                .ToArray();

            var sunData = meshResourceCache.Load(sunMesh.Id, "Sun", () => sunMesh.Vertices, sunMesh.Indices, entry);

            var instances = entry.DisposeAfterExecution(entry.CreateUploadBuffer(byBlueprint
                .SelectMany((group, index) => group.Units
                    .Select(unit =>
                        new Vortice.Direct3D12.RaytracingInstanceDescription
                        {
                            AccelerationStructure = group.Data.BLAS.GPUVirtualAddress,
                            InstanceID = new Vortice.UInt24(0),
                            Flags = Vortice.Direct3D12.RaytracingInstanceFlags.None,
                            Transform = unit.WorldMatrix.AsAffine(),
                            InstanceMask = 0xFF,
                            InstanceContributionToHitGroupIndex = new Vortice.UInt24((uint)index)
                        }))
                .Concat(volume.Map.Suns.Select((s, index) => 
                    new Vortice.Direct3D12.RaytracingInstanceDescription
                    {
                        AccelerationStructure = sunData.BLAS.GPUVirtualAddress,
                        InstanceID = new Vortice.UInt24(0),
                        Flags = Vortice.Direct3D12.RaytracingInstanceFlags.None,
                        InstanceMask = 0xFF,
                        InstanceContributionToHitGroupIndex = new Vortice.UInt24((uint)(byBlueprint.Length + index)),
                        Transform = (Matrix4x4.CreateScale(s.Size) * Matrix4x4.CreateTranslation(s.Position)).AsAffine()
                    }))
                .ToArray()))
                .Name("TLAS prep buffer");

            var asDesc = new Vortice.Direct3D12.BuildRaytracingAccelerationStructureInputs
            {
                DescriptorsCount = volume.Units.Count() + volume.Map.Suns.Count(),
                Flags = Vortice.Direct3D12.RaytracingAccelerationStructureBuildFlags.None,
                Layout = Vortice.Direct3D12.ElementsLayout.Array,
                Type = Vortice.Direct3D12.RaytracingAccelerationStructureType.TopLevel,
                InstanceDescriptions = instances.GPUVirtualAddress,
            };

            var prebuild = device.GetRaytracingAccelerationStructurePrebuildInfo(asDesc);

            var scratch = entry.DisposeAfterExecution(device.CreateStaticBuffer(prebuild.ScratchDataSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.Common, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name("TLAS scratch"));
            var result = entry.DisposeAfterExecution(device.CreateStaticBuffer(prebuild.ResultDataMaxSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.RaytracingAccelerationStructure, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name("TLAS result"));

            entry.List.BuildRaytracingAccelerationStructure(new Vortice.Direct3D12.BuildRaytracingAccelerationStructureDescription
            {
                DestinationAccelerationStructureData = result.GPUVirtualAddress,
                ScratchAccelerationStructureData = scratch.GPUVirtualAddress,
                Inputs = asDesc,
                SourceAccelerationStructureData = 0,
            });

            entry.List.ResourceBarrierUnorderedAccessView(result);
            entry.List.SetComputeRootSignature(emptyGlobalSignature);

            device.CreateShaderResourceView(null,
                new Vortice.Direct3D12.ShaderResourceViewDescription
                {
                    Format = Vortice.DXGI.Format.Unknown,
                    ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.RaytracingAccelerationStructure,
                    Shader4ComponentMapping = Vortice.Direct3D12.ShaderComponentMapping.Default,
                    RaytracingAccelerationStructure = new Vortice.Direct3D12.RaytracingAccelerationStructureShaderResourceView
                    {
                        Location = result.GPUVirtualAddress,
                    }
                },
                srvUavHeap.CPU(1));

            var frustum = Frustum.FromScreen(new ScreenRectangle { Start = new ScreenPosition(0, 0), End = new ScreenPosition(rp.ScreenSize.Width, rp.ScreenSize.Height) }, rp.ScreenSize, camera.InvViewProjection);

            var cameraData = new CameraMatrices { worldBottomLeft = frustum.Points[0], worldTopLeft = frustum.Points[1], worldTopRight = frustum.Points[2], Origin = camera.Position };

            var cameraBuffer = entry.DisposeAfterExecution(entry.CreateUploadBuffer(new[] { cameraData }, 256).Name("Camera buffer"));

            device.CreateConstantBufferView(
                new Vortice.Direct3D12.ConstantBufferViewDescription 
                { 
                    BufferLocation = cameraBuffer.GPUVirtualAddress,
                    SizeInBytes = Marshal.SizeOf<CameraMatrices>().Align(256)
                },
                srvUavHeap.CPU(2));

            var ambientLight = new Light { AmbientLightLevel = volume.Map.AmbientLightLevel };
            var shaderTable = entry.DisposeAfterExecution(new ShaderBindingTable(stateObjectProperties));
            shaderTable.AddRayGeneration("RayGen", BitConverter.GetBytes(srvUavHeap.GPU(0).Ptr));
            foreach (var group in byBlueprint)
            {
                shaderTable.AddHit("ObjectHitGroup", BitConverter.GetBytes(group.Data.VertexBuffer.GPUVirtualAddress).Concat(BitConverter.GetBytes(group.Data.IndexBuffer.GPUVirtualAddress)).Concat(GetBytes(ambientLight)).ToArray());
            }
            foreach (var sun in volume.Map.Suns)
            {
                shaderTable.AddHit("SunHitGroup", GetBytes(new Sun { Colour = sun.LightColour }).ToArray());
            }
            shaderTable.AddMiss("Miss", new byte[0]);

            var dispatchDesc = shaderTable.Create(device, entry);

            entry.List.SetDescriptorHeaps(srvUavHeap);
            entry.List.ResourceBarrierTransition(screenResources.OutputSrv, Vortice.Direct3D12.ResourceStates.CopySource, Vortice.Direct3D12.ResourceStates.UnorderedAccess);

            dispatchDesc.Depth = 1;
            dispatchDesc.Width = rp.ScreenSize.Width;
            dispatchDesc.Height = rp.ScreenSize.Height;

            entry.List.SetPipelineState1(state);

            entry.List.DispatchRays(dispatchDesc);

            entry.List.ResourceBarrierTransition(screenResources.OutputSrv, Vortice.Direct3D12.ResourceStates.UnorderedAccess, Vortice.Direct3D12.ResourceStates.CopySource);
            entry.List.ResourceBarrierTransition(rp.RenderTarget, Vortice.Direct3D12.ResourceStates.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest);

            entry.List.CopyResource(rp.RenderTarget, screenResources.OutputSrv);

            entry.List.ResourceBarrierTransition(rp.RenderTarget, Vortice.Direct3D12.ResourceStates.CopyDest, Vortice.Direct3D12.ResourceStates.RenderTarget);

            entry.Execute();
        }

        private readonly record struct UnitsByBlueprint(Blueprint Blueprint, Unit[] Units, MeshResourceCache.MeshData Data)
        {
        }

        public void Dispose()
        {
            screenResources.Dispose();
            disposeTracker.Dispose();
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct CameraMatrices
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
        private struct Light
        {
            [FieldOffset(0)]
            public float AmbientLightLevel;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct Sun
        {
            [FieldOffset(0)]
            public RGB Colour;
        }

        byte[] GetBytes<T>(T str)
            where T : unmanaged
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }
    }
}
