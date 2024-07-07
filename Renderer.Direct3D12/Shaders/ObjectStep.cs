using Data.Mesh;
using Simulation;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Util;
using static Renderer.Direct3D12.MeshResourceCache;

namespace Renderer.Direct3D12.Shaders
{
    internal class ObjectStep : IRaytracingPipelineStep
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly PrimitiveBlasCache primitiveBlasCache;
        private readonly RandomNumberGenerator rng;
        private readonly MapResourceCache mapResourceCache;

        private readonly MeshResourceCache meshResourceCache;
        private readonly uint maxRays;
        private readonly Raytrace.Hit.ObjectRadiance objectRadiance;
        private readonly Raytrace.Hit.SphereRadiance sphereRadiance;
        private readonly Raytrace.Hit.SphereIntersection sphereIntersection;

        public ObjectStep(MeshResourceCache meshResourceCache, MapResourceCache mapResourceCache, uint maxRays, Raytrace.Hit.ObjectRadiance objectRadiance, Raytrace.Hit.SphereRadiance sphereRadiance, Raytrace.Hit.SphereIntersection sphereIntersection)
        {
            this.meshResourceCache = meshResourceCache;
            this.maxRays = maxRays;
            this.objectRadiance = objectRadiance;
            this.sphereRadiance = sphereRadiance;
            this.sphereIntersection = sphereIntersection;
            this.mapResourceCache = mapResourceCache;
            this.primitiveBlasCache = disposeTracker.Track(new PrimitiveBlasCache());
            this.rng = disposeTracker.Track(RandomNumberGenerator.Create());
        }

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects()
        {
            return [
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "ObjectRadiance",
                    ClosestHitShaderImport = objectRadiance.Export,
                }),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.ProceduralPrimitive,
                    HitGroupExport = "SphereRadiance",
                    IntersectionShaderImport = sphereIntersection.Export,
                    ClosestHitShaderImport = sphereRadiance.Export,
                }),
            ];
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public void PrepareRaytracing(RaytracePreparation preparation)
        {
            var cache = new Dictionary<IGeometry, int>();
            var unitInstances = preparation.Volume.Units
                .Select(u => new InstanceDescription
                {
                    BLAS = GetBLAS(u.Blueprint.Name, u.Blueprint.Mesh, preparation.List),
                    HitGroup = GetHitGroup(cache, preparation.ShaderTable, preparation.HeapAccumulator, u.WorldMatrix, u.Blueprint.Mesh, preparation.List),
                    Transform = u.WorldMatrix
                });

            var predefined = preparation.Volume.Map.Objects
                .Select(o => new InstanceDescription
                {
                    BLAS = GetBLAS(o.Name, o.Geometry, preparation.List),
                    HitGroup = GetHitGroup(cache, preparation.ShaderTable, preparation.HeapAccumulator, o.WorldMatrix, o.Geometry, preparation.List),
                    Transform = o.WorldMatrix
                });

            var instances = unitInstances
                .Concat(predefined)
                .Select(i => new Vortice.Direct3D12.RaytracingInstanceDescription
                {
                    AccelerationStructure = i.BLAS.GPUVirtualAddress,
                    InstanceID = new Vortice.UInt24(0),
                    Flags = Vortice.Direct3D12.RaytracingInstanceFlags.ForceOpaque,
                    Transform = i.Transform.AsAffine(),
                    InstanceMask = 0xFF,
                    InstanceContributionToHitGroupIndex = new Vortice.UInt24((uint)i.HitGroup)
                });

            preparation.InstanceDescriptions.AddRange(instances);
        }

        private int GetHitGroup(Dictionary<IGeometry, int> geometryHitGroups, ShaderBindingTable shaderTable, DescriptorHeapAccumulator heapAccumulator, Matrix4x4 worldMatrix, IGeometry geometry, PooledCommandList list)
        {
            if (geometryHitGroups.ContainsKey(geometry)) return geometryHitGroups[geometry];

            var hitGroup = PrepareHitGroup(shaderTable, heapAccumulator, worldMatrix, geometry, list);
            geometryHitGroups[geometry] = hitGroup;
            return hitGroup;
        }

        private int PrepareHitGroup(ShaderBindingTable shaderTable, DescriptorHeapAccumulator heapAccumulator, Matrix4x4 worldMatrix, IGeometry geometry, PooledCommandList list)
        {
            if (geometry is SphereGeometry sphere)
            {
                return PrepareSphereHitGroup(shaderTable, worldMatrix, sphere);
            }

            if (geometry is Mesh mesh)
            {
                return PrepareMeshHitGroup(shaderTable, heapAccumulator, mesh, list);
            }

            throw new InvalidOperationException();
        }

        private int PrepareSphereHitGroup(ShaderBindingTable shaderTable, Matrix4x4 worldMatrix, SphereGeometry sphere)
        {
            var pos = Vector3.Transform(new Vector3(0, 0, 0), worldMatrix);
            var size = Vector3.TransformNormal(new Vector3(1, 0, 0), worldMatrix).Length();
            var parameters = (Vortice.Direct3D12.ID3D12Resource tlas) => new Data.SphereHitGroupParameters
            {
                WorldPosition = pos,
                Size = size,
                EmissionStrength = sphere.Material.EmissionStrength,
                Colour = sphere.Material.Colour,
                EmissionColour = sphere.Material.EmissionColour,
            }.GetBytes();

            return shaderTable.AddHit("SphereRadiance", parameters);
        }

        private int PrepareMeshHitGroup(ShaderBindingTable shaderTable, DescriptorHeapAccumulator heapAccumulator, Mesh mesh, PooledCommandList list)
        {
            var meshData = meshResourceCache.Load(mesh, list);

            var parameters = (Vortice.Direct3D12.ID3D12Resource tlas) => new Data.ObjectRadianceParameters
            {
                MaxSamples = 4,
                MaxBounces = maxRays,
                Seed = rng.GetRandom<uint>(),
                TLASIndex = heapAccumulator.AddRaytracingStructure(tlas),
                MaterialIndicesIndex = heapAccumulator.AddStructuredBuffer(meshData.MaterialIndexBuffer, meshData.MaterialIndexSRV),
                MaterialsIndex = heapAccumulator.AddStructuredBuffer(meshData.MaterialBuffer, meshData.MaterialSRV),
                VertexIndicesIndex = heapAccumulator.AddStructuredBuffer(meshData.VertexIndexBuffer, meshData.VertexIndexSRV),
                VerticesIndex = heapAccumulator.AddStructuredBuffer(meshData.VertexBuffer, meshData.VertexSRV),
            }.GetBytes();

            return shaderTable.AddHit("ObjectRadiance", parameters);
        }

        private Vortice.Direct3D12.ID3D12Resource GetBLAS(string name, IGeometry geometry, PooledCommandList list)
        {
            if (geometry is SphereGeometry)
            {
                return primitiveBlasCache.Get(list).SphereBlas;
            }

            if (geometry is Mesh mesh)
            {
                return meshResourceCache.Load(mesh, list).BLAS;
            }

            throw new InvalidOperationException();
        }

        private struct InstanceDescription
        {
            public required Vortice.Direct3D12.ID3D12Resource BLAS { get; init; }
            public required Matrix4x4 Transform { get; init; }
            public required int HitGroup { get; init; }
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
        }
    }
}
