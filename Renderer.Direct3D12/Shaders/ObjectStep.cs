using Data.Mesh;
using Simulation;
using System.Numerics;
using System.Security.Cryptography;
using Util;

namespace Renderer.Direct3D12.Shaders
{
    internal class ObjectStep : IRaytracingPipelineStep
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly PrimitiveBlasCache primitiveBlasCache;

        private readonly MeshResourceCache meshResourceCache;
        private readonly uint maxRays;
        private readonly Raytrace.Hit.ObjectRadiance objectRadiance;
        private readonly Raytrace.Hit.SphereRadiance sphereRadiance;
        private readonly Raytrace.Hit.SphereIntersection sphereIntersection; 
        private readonly RandomNumberGenerator rng;

        public ObjectStep(MeshResourceCache meshResourceCache, uint maxRays, Raytrace.Hit.ObjectRadiance objectRadiance, Raytrace.Hit.SphereRadiance sphereRadiance, Raytrace.Hit.SphereIntersection sphereIntersection)
        {
            this.meshResourceCache = meshResourceCache;
            this.maxRays = maxRays;
            this.objectRadiance = objectRadiance;
            this.sphereRadiance = sphereRadiance;
            this.sphereIntersection = sphereIntersection;
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
            var light = preparation.Volume.Units.Select(u => LightSource(u, preparation.HeapAccumulator, preparation.List)).Concat(preparation.Volume.Map.Objects.Select(o => LightSource(o, preparation.HeapAccumulator, preparation.List))).Where(s => s.Power > (Half)0).First();

            var unitInstances = preparation.Volume.Units
                .Select(u => new InstanceDescription
                {
                    BLAS = GetBLAS(u.Blueprint.Name, u.Blueprint.Mesh, preparation.List),
                    HitGroup = GetHitGroup(preparation.ShaderTable, light, preparation.Volume.Map.AmbientLightLevel, preparation.HeapAccumulator, u.WorldMatrix, u.Blueprint.Mesh, preparation.List),
                    Transform = u.WorldMatrix
                });

            var predefined = preparation.Volume.Map.Objects
                .Select(o => new InstanceDescription
                {
                    BLAS = GetBLAS(o.Name, o.Geometry, preparation.List),
                    HitGroup = GetHitGroup(preparation.ShaderTable, light, preparation.Volume.Map.AmbientLightLevel, preparation.HeapAccumulator, o.WorldMatrix, o.Geometry, preparation.List),
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

        private int GetHitGroup(ShaderBindingTable shaderTable, Data.LightSource light, float ambientLight, DescriptorHeapAccumulator heapAccumulator, Matrix4x4 worldMatrix, IGeometry geometry, PooledCommandList list)
        {
            return PrepareHitGroup(shaderTable, light, ambientLight, heapAccumulator, worldMatrix, geometry, list);
        }

        private int PrepareHitGroup(ShaderBindingTable shaderTable, Data.LightSource light, float ambientLight, DescriptorHeapAccumulator heapAccumulator, Matrix4x4 worldMatrix, IGeometry geometry, PooledCommandList list)
        {
            if (geometry is SphereGeometry sphere)
            {
                return PrepareSphereHitGroup(shaderTable, worldMatrix, sphere);
            }

            if (geometry is Mesh mesh)
            {
                return PrepareMeshHitGroup(shaderTable, light, worldMatrix, ambientLight, heapAccumulator, mesh, list);
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
                Size = (Half)size,
                EmissionStrength = (Half)sphere.Material.EmissionStrength,
                Colour = sphere.Material.Colour,
                EmissionColour = sphere.Material.EmissionColour,
            }.GetBytes();

            return shaderTable.AddHit("SphereRadiance", parameters);
        }

        private int PrepareMeshHitGroup(ShaderBindingTable shaderTable, Data.LightSource light, Matrix4x4 worldMatrix, float ambientLight, DescriptorHeapAccumulator heapAccumulator, Mesh mesh, PooledCommandList list)
        {
            var meshData = meshResourceCache.Load(mesh, list);

            var parameters = (Vortice.Direct3D12.ID3D12Resource tlas) => new Data.ObjectRadianceParameters
            {
                AmbientLight = ambientLight,
                MaxBounces = maxRays,
                Seed = rng.GetRandom<uint>(),
                Light = light,
                WorldMatrix = Matrix4x4.Transpose(worldMatrix),
                TLASIndex = heapAccumulator.AddRaytracingStructure(tlas),
                TrianglesIndex = heapAccumulator.AddStructuredBuffer(meshData.TriangleBuffer, meshData.TriangleSRV),
            }.GetBytes();

            return shaderTable.AddHit("ObjectRadiance", parameters);
        }

        private Shaders.Data.LightSource LightSource(Unit unit, DescriptorHeapAccumulator heapAccumulator, PooledCommandList list)
        {
            return LightSource(unit.Blueprint.Mesh, heapAccumulator, list, unit.WorldMatrix);
        }

        private Shaders.Data.LightSource LightSource(PredefinedObject o, DescriptorHeapAccumulator heapAccumulator, PooledCommandList list)
        {
            if (o.Geometry is Mesh mesh)
            {
                return LightSource(mesh, heapAccumulator, list, o.WorldMatrix);
            }

            if (o.Geometry is SphereGeometry sphere)
            {
                return LightSource(sphere, heapAccumulator, list, o.WorldMatrix);
            }

            throw new InvalidOperationException();
        }

        private Shaders.Data.LightSource LightSource(SphereGeometry sphere, DescriptorHeapAccumulator heapAccumulator, PooledCommandList list, Matrix4x4 worldMatrix)
        {
            var size = Vector3.TransformNormal(new Vector3(1, 0, 0), worldMatrix).Length();
            var pos = Vector3.Transform(new Vector3(0, 0, 0), worldMatrix);
            return new Data.LightSource
            {
                DistanceIndependent = sphere.DistanceIndependentEmission,
                Size = (Half)size,
                Position = pos,
                Power = (Half)sphere.Material.EmissionStrength,
                TrianglesIndex = 0,
                VerticesIndex = 0,
                WorldMatrix = Matrix4x4.Transpose(worldMatrix)
            };
        }

        private Shaders.Data.LightSource LightSource(Mesh mesh, DescriptorHeapAccumulator heapAccumulator, PooledCommandList list, Matrix4x4 worldMatrix)
        {
            var pos = Vector3.Transform(new Vector3(0, 0, 0), worldMatrix);
            var meshData = meshResourceCache.Load(mesh, list);
            return new Data.LightSource
            {
                DistanceIndependent = false,
                Size = (Half)meshData.Size,
                Position = pos,
                Power = (Half)meshData.Power,
                TrianglesIndex = heapAccumulator.AddStructuredBuffer(meshData.TriangleBuffer, meshData.TriangleSRV),
                WorldMatrix = Matrix4x4.Transpose(worldMatrix),
                VerticesIndex = 0
            };
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
