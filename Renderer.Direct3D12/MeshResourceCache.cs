using Data.Mesh;
using Simulation.Physics;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    internal class MeshResourceCache
    {
        private readonly Dictionary<Mesh, MeshData> cache = new Dictionary<Mesh, MeshData>();

        public MeshData Load(Mesh mesh, FrameResources frameResources)
        {
            if (cache.ContainsKey(mesh)) return cache[mesh];
            var name = mesh.Name;

            var vertices = mesh.Vertices.Select(x => new Shaders.Data.Vertex { Normal = x.Normal, Position = x.Position }).ToArray();
            var triangles = mesh.Triangles.Select(x => new Shaders.Data.Triangle { Colour = mesh.Materials[x.MaterialIndex].Colour, EmissionColour = mesh.Materials[x.MaterialIndex].EmissionColour, EmissionStrength = mesh.Materials[x.MaterialIndex].EmissionStrength, Normal = Normal(mesh, x), pad0 = 0, pad1 = 0 }).ToArray();
            var totalPower = mesh.Triangles.Sum(t => Power(mesh, t));
            var aabb = AABB.FromVertices(mesh.Vertices.Select(x => x.Position));
            var size = Math.Max(aabb.Start.X - aabb.End.X, Math.Max(aabb.Start.Y - aabb.End.Y, aabb.Start.Z - aabb.End.Z));

            var vertexBuffer = frameResources.TransferToUpload(vertices);
            var triangleBuffer = frameResources.Permanent.UploadReadonly(frameResources.UploadBufferPool, triangles);

            // The documentation states that this needs only natural alignment. nVidia actually requires 16 alignment.
            var indexBuffer = frameResources.TransferToUpload(mesh.Triangles.SelectMany(m => m.Vertices).ToArray(), 16);

            var blas = frameResources.BuildAS(frameResources.Permanent.BLASPool, new Vortice.Direct3D12.BuildRaytracingAccelerationStructureInputs
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
                            IndexCount = indexBuffer.NumElements,
                            IndexFormat = IndexFormat(mesh.Triangles[0].Vertices[0].GetType()),
                            VertexBuffer = new Vortice.Direct3D12.GpuVirtualAddressAndStride
                            {
                                StartAddress = vertexBuffer.GPUVirtualAddress,
                                StrideInBytes = (uint)Marshal.SizeOf(vertices[0].GetType()),
                            },
                            VertexCount = (uint)mesh.Vertices.Length,
                            VertexFormat = Vortice.DXGI.Format.R32G32B32_Float,
                            Transform3x4 = 0,
                        },
                        Type = Vortice.Direct3D12.RaytracingGeometryType.Triangles
                    }
                ],
                InstanceDescriptions = 0,
                Flags = Vortice.Direct3D12.RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
                Layout = Vortice.Direct3D12.ElementsLayout.Array,
                Type = Vortice.Direct3D12.RaytracingAccelerationStructureType.BottomLevel
            });

            var data = new MeshData
            {
                BLAS = blas,
                Triangles = triangleBuffer,
                Power = totalPower,
                Size = size
            };

            cache[mesh] = data;

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

        public class MeshData
        {
            public required BufferView Triangles { get; init; }
            public required BufferView BLAS { get; init; }
            public required float Power { get; init; }
            public required float Size { get; init; }
        }
    }
}
