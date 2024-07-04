using Data.Mesh;
using Data.Space;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    internal class MeshResourceCache : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Dictionary<Guid, MeshData> cache = new Dictionary<Guid, MeshData>();

        private readonly Vortice.Direct3D12.ID3D12Device5 device;

        public MeshResourceCache(Vortice.Direct3D12.ID3D12Device5 device)
        {
            this.device = device;
        }

        public MeshData Load(string name, Mesh mesh, PooledCommandList list)
        {
            if (cache.ContainsKey(mesh.Id)) return cache[mesh.Id];

            var vertices = mesh.Vertices.Select(x => new Shaders.Data.Vertex { Normal = x.Normal, Position = x.Position }).ToArray();
            var vertexIndices = mesh.Triangles.SelectMany(x => x.Vertices).ToArray();
            var materialIndices = mesh.Triangles.Select(x => (uint)x.MaterialIndex).ToArray();
            var materials = mesh.Materials.Select(m => new Shaders.Data.Material { Colour = m.Colour, EmissionColour = m.EmissionColour, EmissionStrength = m.EmissionStrength }).ToArray();

            var vertexBuffer = disposeTracker.Track(device.CreateStaticBuffer(vertices.SizeOf()).Name($"{name} vertex buffer"));
            var indexBuffer = disposeTracker.Track(device.CreateStaticBuffer(vertexIndices.SizeOf()).Name($"{name} vertex index buffer"));
            var materialIndexBuffer = disposeTracker.Track(device.CreateStaticBuffer(materialIndices.SizeOf())).Name($"{name} material index buffer");
            var materialBuffer = disposeTracker.Track(device.CreateStaticBuffer(materials.SizeOf())).Name($"{name} material buffer");

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
                            IndexCount = vertexIndices.Length,
                            IndexFormat = IndexFormat(vertexIndices[0].GetType()),
                            VertexBuffer = new Vortice.Direct3D12.GpuVirtualAddressAndStride
                            {
                                StartAddress = vertexBuffer.GPUVirtualAddress,
                                StrideInBytes = (uint)Marshal.SizeOf(vertices[0].GetType()),
                            },
                            VertexCount = mesh.Vertices.Length,
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

            list.UploadData(vertexBuffer, vertices);
            list.UploadData(indexBuffer, vertexIndices);
            list.UploadData(materialIndexBuffer, materialIndices);
            list.UploadData(materialBuffer, materials);

            var prebuild = device.GetRaytracingAccelerationStructurePrebuildInfo(asDesc);

            var scratch = list.DisposeAfterExecution(device.CreateStaticBuffer(prebuild.ScratchDataSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.Common, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name($"{name} BLAS scratch"));
            var result = disposeTracker.Track(device.CreateStaticBuffer(prebuild.ResultDataMaxSizeInBytes.Align(256), Vortice.Direct3D12.ResourceStates.RaytracingAccelerationStructure, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess).Name($"{name} BLAS"));

            list.List.BuildRaytracingAccelerationStructure(new Vortice.Direct3D12.BuildRaytracingAccelerationStructureDescription
            {
                DestinationAccelerationStructureData = result.GPUVirtualAddress,
                ScratchAccelerationStructureData = scratch.GPUVirtualAddress,
                Inputs = asDesc,
                SourceAccelerationStructureData = 0,
            });

            var data = new MeshData
            { 
                BLAS = result, 
                VertexIndexBuffer = indexBuffer,
                VertexIndexSRV = new Vortice.Direct3D12.BufferShaderResourceView
                {
                    StructureByteStride = Marshal.SizeOf<uint>(),
                    NumElements = vertexIndices.Length,
                },
                VertexBuffer = vertexBuffer,
                VertexSRV  = new Vortice.Direct3D12.BufferShaderResourceView
                {
                    StructureByteStride = Marshal.SizeOf<Shaders.Data.Vertex>(),
                    NumElements = vertices.Length,
                },
                MaterialBuffer = materialBuffer,
                MaterialSRV = new Vortice.Direct3D12.BufferShaderResourceView 
                {
                    StructureByteStride = Marshal.SizeOf<Shaders.Data.Material>(),
                    NumElements = materials.Length,
                },
                MaterialIndexBuffer = materialIndexBuffer,
                MaterialIndexSRV = new Vortice.Direct3D12.BufferShaderResourceView
                {
                    StructureByteStride = Marshal.SizeOf<uint>(),
                    NumElements = materialIndices.Length,
                }
            };

            cache[mesh.Id] = data;

            list.List.ResourceBarrierUnorderedAccessView(result);

            return cache[mesh.Id];
        }

        private Vortice.DXGI.Format IndexFormat(Type type)
        {
            if (type == typeof(short) || type == typeof(ushort))
            {
                return Vortice.DXGI.Format.R16_UInt;
            }

            if (type == typeof(int) || type == typeof(uint))
            {
                return Vortice.DXGI.Format.R32_UInt;
            }

            throw new InvalidOperationException();
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public class MeshData
        {
            public required Vortice.Direct3D12.ID3D12Resource VertexBuffer { get; init; }
            public required Vortice.Direct3D12.BufferShaderResourceView VertexSRV { get; init; }
            public required Vortice.Direct3D12.ID3D12Resource VertexIndexBuffer { get; init; }
            public required Vortice.Direct3D12.BufferShaderResourceView VertexIndexSRV { get; init; }
            public required Vortice.Direct3D12.ID3D12Resource MaterialIndexBuffer { get; init; }
            public required Vortice.Direct3D12.BufferShaderResourceView MaterialIndexSRV { get; init; }
            public required Vortice.Direct3D12.ID3D12Resource MaterialBuffer { get; init; }
            public required Vortice.Direct3D12.BufferShaderResourceView MaterialSRV { get; init; }
            public required Vortice.Direct3D12.ID3D12Resource BLAS { get; init; }
        }
    }
}
