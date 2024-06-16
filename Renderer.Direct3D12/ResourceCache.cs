using Simulation;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    internal class ResourceCache : IDisposable
    {
        private readonly VertexCalculator vertexCalculator = new VertexCalculator();
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Dictionary<Blueprint, BlueprintData> cache = new Dictionary<Blueprint, BlueprintData>();

        private readonly Vortice.Direct3D12.ID3D12Device5 device;

        public ResourceCache(Vortice.Direct3D12.ID3D12Device5 device)
        {
            this.device = device;
        }

        public BlueprintData For(Blueprint blueprint, PooledCommandList list, bool raytrace)
        {
            var existing = Load(blueprint, list);
            if (raytrace && existing.RaytracingData == null)
            {
                PrepareRaytracing(blueprint, existing, list);
            }

            return existing;
        }

        private BlueprintData Load(Blueprint blueprint, PooledCommandList list)
        {
            if (cache.ContainsKey(blueprint)) return cache[blueprint];

            var verts = vertexCalculator.CalculateVertices(blueprint.Mesh);

            var data = new BlueprintData
            {
                VertexBuffer = disposeTracker.Track(device.CreateStaticBuffer(verts.SizeOf()).Name($"Vertex buffer for {blueprint.Name}")),
                VertexBufferSize = verts.SizeOf(),
                VertexBufferStride = (uint)Marshal.SizeOf(verts[0].GetType()),
                IndexBufferFormat = Vortice.DXGI.Format.R32_UInt,
                IndexBufferSize = blueprint.Mesh.Indices.SizeOf(),
                IndexBuffer = disposeTracker.Track(device.CreateStaticBuffer(blueprint.Mesh.Indices.SizeOf()).Name($"Index buffer for {blueprint.Name}")),
            };

            cache[blueprint] = data;

            list.UploadData(data.VertexBuffer, verts);
            list.UploadData(data.IndexBuffer, blueprint.Mesh.Indices);

            return cache[blueprint];
        }

        private void PrepareRaytracing(Blueprint blueprint, BlueprintData data, PooledCommandList list)
        {
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
                            IndexBuffer = data.IndexBuffer.GPUVirtualAddress,
                            IndexCount = blueprint.Mesh.Indices.Length,
                            IndexFormat = data.IndexBufferFormat,
                            VertexBuffer = new Vortice.Direct3D12.GpuVirtualAddressAndStride
                            {
                                StartAddress = data.VertexBuffer.GPUVirtualAddress,
                                StrideInBytes = data.VertexBufferStride,
                            },
                            VertexCount = blueprint.Mesh.Vertices.Length,
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

            var prebuild = device.GetRaytracingAccelerationStructurePrebuildInfo(asDesc);

            var scratch = list.DisposeAfterExecution(device.CreateStaticBuffer(prebuild.ScratchDataSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.Common, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name($"BLAS scratch {blueprint.Name}"));
            var result = disposeTracker.Track(device.CreateStaticBuffer(prebuild.ResultDataMaxSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.RaytracingAccelerationStructure, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name($"BLAS result {blueprint.Name}"));

            list.List.BuildRaytracingAccelerationStructure(new Vortice.Direct3D12.BuildRaytracingAccelerationStructureDescription
            {
                DestinationAccelerationStructureData = result.GPUVirtualAddress,
                ScratchAccelerationStructureData = scratch.GPUVirtualAddress,
                Inputs = asDesc,
                SourceAccelerationStructureData = 0,
            });

            list.List.ResourceBarrierUnorderedAccessView(result);

            data.RaytracingData = new RaytracingData
            {
                BLAS = result
            };
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public class BlueprintData
        {
            public required Vortice.Direct3D12.ID3D12Resource VertexBuffer { get; init; }
            public required uint VertexBufferSize { get; init; }
            public required uint VertexBufferStride { get; init; }
            public required Vortice.Direct3D12.ID3D12Resource IndexBuffer { get; init; }
            public required uint IndexBufferSize { get; init; }
            public required Vortice.DXGI.Format IndexBufferFormat { get; init; }
            public RaytracingData? RaytracingData { get; set; }
        }

        public class RaytracingData
        {
            public required Vortice.Direct3D12.ID3D12Resource BLAS { get; init; }
        }
    }
}
