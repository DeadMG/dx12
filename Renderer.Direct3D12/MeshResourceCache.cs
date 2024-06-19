using Simulation;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    internal class MeshResourceCache : IDisposable
    {
        private readonly VertexCalculator vertexCalculator = new VertexCalculator();
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Dictionary<Guid, MeshData> cache = new Dictionary<Guid, MeshData>();

        private readonly Vortice.Direct3D12.ID3D12Device5 device;

        public MeshResourceCache(Vortice.Direct3D12.ID3D12Device5 device)
        {
            this.device = device;
        }

        public MeshData Load(Mesh mesh, PooledCommandList list)
        {
            if (cache.ContainsKey(mesh.Id)) return cache[mesh.Id];

            var verts = vertexCalculator.CalculateVertices(mesh);

            var vertexBuffer = disposeTracker.Track(device.CreateStaticBuffer(verts.SizeOf()).Name($"Vertex buffer for {mesh.Id}"));
            var indexBuffer = disposeTracker.Track(device.CreateStaticBuffer(mesh.Indices.SizeOf()).Name($"Index buffer for {mesh.Id}"));

            var asDesc = new Vortice.Direct3D12.BuildRaytracingAccelerationStructureInputs
            {
                DescriptorsCount = 1,
                GeometryDescriptions =
                [
                    new Vortice.Direct3D12.RaytracingGeometryDescription
                    {
                        AABBs = new Vortice.Direct3D12.RaytracingGeometryAabbsDescription
                        {
                        },
                        Flags = Vortice.Direct3D12.RaytracingGeometryFlags.Opaque,
                        Triangles = new Vortice.Direct3D12.RaytracingGeometryTrianglesDescription
                        {
                            IndexBuffer = indexBuffer.GPUVirtualAddress,
                            IndexCount = mesh.Indices.Length,
                            IndexFormat = Vortice.DXGI.Format.R32_UInt,
                            VertexBuffer = new Vortice.Direct3D12.GpuVirtualAddressAndStride
                            {
                                StartAddress = vertexBuffer.GPUVirtualAddress,
                                StrideInBytes = (uint)Marshal.SizeOf(verts[0].GetType()),
                            },
                            VertexCount = verts.Length,
                            VertexFormat = Vortice.DXGI.Format.R32G32B32_Float,
                            Transform3x4 = 0,
                        },
                        Type = Vortice.Direct3D12.RaytracingGeometryType.Triangles
                    }
                ],
                InstanceDescriptions = 0,
                Flags = Vortice.Direct3D12.RaytracingAccelerationStructureBuildFlags.None,
                Layout = Vortice.Direct3D12.ElementsLayout.Array,
                Type = Vortice.Direct3D12.RaytracingAccelerationStructureType.BottomLevel
            };

            list.UploadData(vertexBuffer, verts);
            list.UploadData(indexBuffer, mesh.Indices);

            var prebuild = device.GetRaytracingAccelerationStructurePrebuildInfo(asDesc);

            var scratch = list.DisposeAfterExecution(device.CreateStaticBuffer(prebuild.ScratchDataSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.Common, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name($"BLAS scratch {mesh.Id}"));
            var result = disposeTracker.Track(device.CreateStaticBuffer(prebuild.ResultDataMaxSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.RaytracingAccelerationStructure, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name($"BLAS result {mesh.Id}"));

            list.List.BuildRaytracingAccelerationStructure(new Vortice.Direct3D12.BuildRaytracingAccelerationStructureDescription
            {
                DestinationAccelerationStructureData = result.GPUVirtualAddress,
                ScratchAccelerationStructureData = scratch.GPUVirtualAddress,
                Inputs = asDesc,
                SourceAccelerationStructureData = 0,
            });

            var data = new MeshData { BLAS = result, IndexBuffer = indexBuffer, VertexBuffer = vertexBuffer };

            cache[mesh.Id] = data;

            list.List.ResourceBarrierUnorderedAccessView(result);

            return cache[mesh.Id];
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public class MeshData
        {
            public required Vortice.Direct3D12.ID3D12Resource VertexBuffer { get; init; }
            public required Vortice.Direct3D12.ID3D12Resource IndexBuffer { get; init; }
            public required Vortice.Direct3D12.ID3D12Resource BLAS { get; init; }
        }
    }
}
