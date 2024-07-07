
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    internal class PrimitiveBlasCache : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private PrimitiveBlas? cacheData;

        public PrimitiveBlas Get(PooledCommandList list)
        {
            if (cacheData != null) return cacheData;

            var sphereAabb = list.CreateUploadBuffer([new PrimitiveAabb { Start = new Vector3(-1, -1, -1), End = new Vector3(1, 1, 1) }]);

            var asDesc = new Vortice.Direct3D12.BuildRaytracingAccelerationStructureInputs
            {
                DescriptorsCount = 1,
                GeometryDescriptions =
                [
                    new Vortice.Direct3D12.RaytracingGeometryDescription
                    {
                        AABBs = new Vortice.Direct3D12.RaytracingGeometryAabbsDescription
                        {
                            AABBCount = 1,
                            AABBs = new Vortice.Direct3D12.GpuVirtualAddressAndStride 
                            {
                                StartAddress = sphereAabb.GPUVirtualAddress,
                                StrideInBytes = (ulong)Marshal.SizeOf<PrimitiveAabb>()
                            },
                        },
                        Flags = Vortice.Direct3D12.RaytracingGeometryFlags.Opaque,
                        Type = Vortice.Direct3D12.RaytracingGeometryType.ProceduralPrimitiveAabbs
                    }
                ],
                InstanceDescriptions = 0,
                Flags = Vortice.Direct3D12.RaytracingAccelerationStructureBuildFlags.None,
                Layout = Vortice.Direct3D12.ElementsLayout.Array,
                Type = Vortice.Direct3D12.RaytracingAccelerationStructureType.BottomLevel
            };

            var prebuild = list.Pool.Device.GetRaytracingAccelerationStructurePrebuildInfo(asDesc);
            var scratch = list.DisposeAfterExecution(list.Pool.Device.CreateStaticBuffer(prebuild.ScratchDataSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.Common, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name($"Sphere BLAS scratch"));
            var result = disposeTracker.Track(list.Pool.Device.CreateStaticBuffer(prebuild.ResultDataMaxSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.RaytracingAccelerationStructure, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name($"Sphere BLAS"));

            list.List.BuildRaytracingAccelerationStructure(new Vortice.Direct3D12.BuildRaytracingAccelerationStructureDescription
            {
                DestinationAccelerationStructureData = result.GPUVirtualAddress,
                ScratchAccelerationStructureData = scratch.GPUVirtualAddress,
                Inputs = asDesc,
                SourceAccelerationStructureData = 0,
            });

            list.List.ResourceBarrierUnorderedAccessView(result);

            cacheData = new PrimitiveBlas { SphereBlas = result };
            return cacheData;
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct PrimitiveAabb
    {
        [FieldOffset(0)]
        public Vector3 Start;

        [FieldOffset(12)]
        public Vector3 End;
    }

    internal class PrimitiveBlas
    {
        public required Vortice.Direct3D12.ID3D12Resource SphereBlas { get; init; }
    }
}
