using Data.Space;
using Simulation;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    internal class RaytracingVolumeRenderer : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly ResourceCache resourceCache;
        private readonly CommandListPool directListPool;
        private readonly Vortice.Direct3D12.ID3D12Device5 device;
        private readonly Vortice.DXGI.Format renderTargetFormat;

        private readonly Vortice.Direct3D12.ID3D12DescriptorHeap srvUavHeap;
        private readonly Vortice.Direct3D12.ID3D12RootSignature rayGenSignature;
        private readonly Vortice.Direct3D12.ID3D12RootSignature hitSignature;
        private readonly Vortice.Direct3D12.ID3D12RootSignature emptySignature;
        private readonly Vortice.Direct3D12.ID3D12StateObject state;
        private readonly StateObjectProperties stateObjectProperties;

        private RaytracingScreenResources screenResources;

        public RaytracingVolumeRenderer(ResourceCache resourceCache, Vortice.Direct3D12.ID3D12Device5 device, CommandListPool directListPool, ScreenSize screenSize, Vortice.DXGI.Format renderTargetFormat)
        {
            this.resourceCache = resourceCache;
            this.directListPool = directListPool;
            this.device = device;
            this.renderTargetFormat = renderTargetFormat;

            var flags = Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature;

            var tableParameter = new Vortice.Direct3D12.RootParameter1(
                new Vortice.Direct3D12.RootDescriptorTable1(
                    new Vortice.Direct3D12.DescriptorRange1(Vortice.Direct3D12.DescriptorRangeType.UnorderedAccessView, 1, 0),
                    new Vortice.Direct3D12.DescriptorRange1(Vortice.Direct3D12.DescriptorRangeType.ShaderResourceView, 1, 0),
                    new Vortice.Direct3D12.DescriptorRange1(Vortice.Direct3D12.DescriptorRangeType.ConstantBufferView, 1, 0)),
                Vortice.Direct3D12.ShaderVisibility.All);

            rayGenSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(flags, [tableParameter])).Name("Ray gen root signature"));

            var verticesParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 0 }, Vortice.Direct3D12.ShaderVisibility.All);
            var indicesParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 1 }, Vortice.Direct3D12.ShaderVisibility.All);

            hitSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(flags, [verticesParameter, indicesParameter])).Name("Hit signature"));
            emptySignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(flags)).Name("Empty signature"));

            var most = Shader.LoadDxil("raytrace.hlsl", "lib_6_3");
            var hit = Shader.LoadDxil("hit.hlsl", "lib_6_3");

            var shaderConfigSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.RaytracingShaderConfig { MaxAttributeSizeInBytes = 8, MaxPayloadSizeInBytes = 16 });
            var rayGenSignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(rayGenSignature));
            var hitSignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(hitSignature));
            var emptySignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(emptySignature));

            state = disposeTracker.Track(device.CreateStateObject(new Vortice.Direct3D12.StateObjectDescription(Vortice.Direct3D12.StateObjectType.RaytracingPipeline,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(most,
                     new Vortice.Direct3D12.ExportDescription("RayGen"),
                     new Vortice.Direct3D12.ExportDescription("Miss"))),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(hit,
                     new Vortice.Direct3D12.ExportDescription("ClosestHit"))),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "HitGroup",
                    ClosestHitShaderImport = "ClosestHit",
                }),
                shaderConfigSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(shaderConfigSubobject, "ClosestHit", "RayGen", "Miss")),
                rayGenSignatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(rayGenSignatureSubobject, "RayGen")),
                hitSignatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(hitSignatureSubobject, "HitGroup")),
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
                    Data = resourceCache.For(unitGroup.Key, entry, true)
                })
                .ToArray();

            var instances = entry.DisposeAfterExecution(entry.CreateUploadBuffer(byBlueprint
                .SelectMany((group, index) => group.Units
                    .Select(unit => {
                        var worldMatrix = Matrix4x4.Transpose(unit.WorldMatrix); // HLSL uses the opposite convention
                        return new Vortice.Direct3D12.RaytracingInstanceDescription
                        {
                            AccelerationStructure = group.Data.RaytracingData.BLAS.GPUVirtualAddress,
                            InstanceID = new Vortice.UInt24(0),
                            Flags = Vortice.Direct3D12.RaytracingInstanceFlags.None,
                            Transform = new Vortice.Mathematics.Matrix3x4(
                                worldMatrix.M11, worldMatrix.M12, worldMatrix.M13, worldMatrix.M14,
                                worldMatrix.M21, worldMatrix.M22, worldMatrix.M23, worldMatrix.M24,
                                worldMatrix.M31, worldMatrix.M32, worldMatrix.M33, worldMatrix.M34),
                            InstanceMask = 0xFF,
                            InstanceContributionToHitGroupIndex = new Vortice.UInt24((uint)index)
                        };
                    }))
                .ToArray()));

            var asDesc = new Vortice.Direct3D12.BuildRaytracingAccelerationStructureInputs
            {
                DescriptorsCount = byBlueprint.Sum(x => x.Units.Length),
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

            var cameraBuffer = entry.DisposeAfterExecution(entry.CreateUploadBuffer(new[] { new CameraMatrices { InverseView = camera.InvView, InverseProjection = camera.InvProjection, Origin = camera.Position } }, 256));

            device.CreateConstantBufferView(
                new Vortice.Direct3D12.ConstantBufferViewDescription 
                { 
                    BufferLocation = cameraBuffer.GPUVirtualAddress,
                    SizeInBytes = Marshal.SizeOf<CameraMatrices>().Align(256)
                },
                srvUavHeap.CPU(2));

            var shaderTable = entry.DisposeAfterExecution(new ShaderBindingTable(stateObjectProperties));
            shaderTable.AddRayGeneration("RayGen", BitConverter.GetBytes(srvUavHeap.GPU(0).Ptr));
            foreach (var group in byBlueprint)
            {
                shaderTable.AddHit("HitGroup", BitConverter.GetBytes(group.Data.VertexBuffer.GPUVirtualAddress).Concat(BitConverter.GetBytes(group.Data.IndexBuffer.GPUVirtualAddress)).ToArray());
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

        private readonly record struct UnitsByBlueprint(Blueprint Blueprint, Unit[] Units, ResourceCache.BlueprintData Data)
        {
        }

        public void Dispose()
        {
            screenResources.Dispose();
            disposeTracker.Dispose();
        }

        private struct CameraMatrices
        {
            public Matrix4x4 InverseView;
            public Matrix4x4 InverseProjection;
            public Vector3 Origin;
        }
    }
}
