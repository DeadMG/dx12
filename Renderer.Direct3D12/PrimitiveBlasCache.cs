using System.Numerics;
using System.Runtime.InteropServices;

namespace Renderer.Direct3D12
{
    internal class PrimitiveBlasCache
    {
        private PrimitiveBlas? cacheData;

        public PrimitiveBlas Get(FrameResources resources)
        {
            if (cacheData != null) return cacheData;

            var sphereAabb = resources.TransferToUpload([new PrimitiveAabb { Start = new Vector3(-1, -1, -1), End = new Vector3(1, 1, 1) }], 16);

            cacheData = new PrimitiveBlas 
            {
                SphereBlas = resources.BuildAS(resources.Permanent.BLASPool, new Vortice.Direct3D12.BuildRaytracingAccelerationStructureInputs
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
                    Flags = Vortice.Direct3D12.RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
                    Layout = Vortice.Direct3D12.ElementsLayout.Array,
                    Type = Vortice.Direct3D12.RaytracingAccelerationStructureType.BottomLevel
                })
            };

            return cacheData;
        }
    }

    // Must be aligned to 16 bytes
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal struct PrimitiveAabb
    {
        [FieldOffset(0)]
        public Vector3 Start;

        [FieldOffset(12)]
        public Vector3 End;
    }

    internal class PrimitiveBlas
    {
        public required BufferView SphereBlas { get; init; }
    }
}
