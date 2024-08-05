using Data.Mesh;
using Simulation.Physics;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    internal class MeshResourceCache : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Dictionary<Mesh, MeshData> cache = new Dictionary<Mesh, MeshData>();

        private readonly Vortice.Direct3D12.ID3D12Device5 device;

        public MeshResourceCache(Vortice.Direct3D12.ID3D12Device5 device)
        {
            this.device = device;
        }

        public MeshData Load(Mesh mesh, PooledCommandList list)
        {
            if (cache.ContainsKey(mesh)) return cache[mesh];
            var name = mesh.Name;

            var vertices = mesh.Vertices.Select(x => new Shaders.Data.Vertex { Normal = x.Normal, Position = x.Position }).ToArray();
            var triangles = mesh.Triangles.Select(x => new Shaders.Data.Triangle { Colour = mesh.Materials[x.MaterialIndex].Colour, EmissionColour = mesh.Materials[x.MaterialIndex].EmissionColour, EmissionStrength = (Half)mesh.Materials[x.MaterialIndex].EmissionStrength, Normal = Normal(mesh, x), pad0 = (Half)0, pad1 = (Half)0 }).ToArray();
            var totalPower = mesh.Triangles.Sum(t => Power(mesh, t));
            var aabb = AABB.FromVertices(mesh.Vertices.Select(x => x.Position));
            var size = Math.Max(aabb.Start.X - aabb.End.X, Math.Max(aabb.Start.Y - aabb.End.Y, aabb.Start.Z - aabb.End.Z));

            var vertexBuffer = disposeTracker.Track(device.CreateStaticBuffer(vertices.SizeOf()).Name($"{name} vertex buffer"));
            var triangleBuffer = disposeTracker.Track(device.CreateStaticBuffer(triangles.SizeOf()).Name($"{name} vertex index buffer"));

            var indices = mesh.Triangles.SelectMany(m => m.Vertices).ToArray();
            var indexBuffer = list.DisposeAfterExecution(list.CreateUploadBuffer(indices));

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
                            IndexCount = indices.Length,
                            IndexFormat = IndexFormat(mesh.Triangles[0].Vertices[0].GetType()),
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

            list.UploadData(triangleBuffer, triangles);
            list.UploadData(vertexBuffer, vertices);

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
                TriangleBuffer = triangleBuffer,
                TriangleSRV = new Vortice.Direct3D12.BufferShaderResourceView
                {
                    StructureByteStride = Marshal.SizeOf<Shaders.Data.Triangle>(),
                    NumElements = triangles.Length,
                },
                Power = totalPower,
                Size = size
            };

            cache[mesh] = data;

            list.List.ResourceBarrierUnorderedAccessView(result);

            return data;
        }

        private Vector3 Normal(Mesh mesh, Triangle triangle)
        {
            var a = mesh.Vertices[triangle.Vertices[0]].Position;
            var b = mesh.Vertices[triangle.Vertices[1]].Position;
            var c = mesh.Vertices[triangle.Vertices[2]].Position;

            return Vector3.Normalize(Vector3.Cross(-(b - a), c - a));
        }

        private float Power(Mesh mesh, Triangle triangle)
        {
            var a = mesh.Vertices[triangle.Vertices[0]].Position;
            var b = mesh.Vertices[triangle.Vertices[1]].Position;
            var c = mesh.Vertices[triangle.Vertices[2]].Position;

            var sizeWeight = Vector3.Cross(b - a, c - a).Length();

            return sizeWeight * (mesh.Materials[triangle.MaterialIndex].EmissionStrength);
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
            public required Vortice.Direct3D12.ID3D12Resource TriangleBuffer { get; init; }
            public required Vortice.Direct3D12.BufferShaderResourceView TriangleSRV { get; init; }
            public required Vortice.Direct3D12.ID3D12Resource BLAS { get; init; }
            public required float Power { get; init; }
            public required float Size { get; init; }
        }
    }
}
