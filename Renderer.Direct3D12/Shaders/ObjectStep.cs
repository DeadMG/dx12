using Data.Mesh;
using Renderer.Direct3D12;
using Simulation;
using System.Numerics;
using System.Security.Cryptography;
using Util;
using static Renderer.Direct3D12.ScreenSizeRaytraceResources;

namespace Renderer.Direct3D12.Shaders
{
    internal class ObjectStep : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly uint maxRays;
        private readonly Raytrace.Hit.ObjectRadiance objectRadiance;
        private readonly Raytrace.Hit.SphereRadiance sphereRadiance;
        private readonly Raytrace.Hit.SphereIntersection sphereIntersection; 
        private readonly RandomNumberGenerator rng;

        public ObjectStep(uint maxRays, Raytrace.Hit.ObjectRadiance objectRadiance, Raytrace.Hit.SphereRadiance sphereRadiance, Raytrace.Hit.SphereIntersection sphereIntersection)
        {
            this.maxRays = maxRays;
            this.objectRadiance = objectRadiance;
            this.sphereRadiance = sphereRadiance;
            this.sphereIntersection = sphereIntersection;
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

        public IEnumerable<Vortice.Direct3D12.RaytracingInstanceDescription> PrepareRaytracing(Volume volume, uint tlasIndex, FrameResources resources, ShaderBindingTable table, ResourcePool.ResourceLifetime<IlluminanceTextureKey> illuminanceTexture, ResourcePool.ResourceLifetime<AtrousDataTextureKey> atrous, ResourcePool.ResourceLifetime<GBufferKey> data, ResourcePool.ResourceLifetime<IlluminanceTextureKey>? previousIlluminance)
        {
            var lights = volume.Units.Select(u => LightSource(u, resources)).Concat(volume.Map.Objects.Select(o => LightSource(o, resources))).Where(s => s.Power > 0).ToArray();
            var lightBuffer = resources.TransferToUpload(lights);
            var lightIndex = resources.HeapAccumulator.AddStructuredBuffer(lightBuffer);

            var unitInstances = volume.Units
                .Select(u => new InstanceDescription
                {
                    BLAS = GetBLAS(u.Blueprint.Name, u.Blueprint.Mesh, resources),
                    HitGroup = GetHitGroup(table, lightIndex, tlasIndex, data, atrous, illuminanceTexture, previousIlluminance, volume.Map.AmbientLightLevel, u.WorldMatrix, u.Blueprint.Mesh, resources),
                    Transform = u.WorldMatrix
                });

            var predefined = volume.Map.Objects
                .Select(o => new InstanceDescription
                {
                    BLAS = GetBLAS(o.Name, o.Geometry, resources),
                    HitGroup = GetHitGroup(table, lightIndex, tlasIndex, data, atrous, illuminanceTexture, previousIlluminance, volume.Map.AmbientLightLevel, o.WorldMatrix, o.Geometry, resources),
                    Transform = o.WorldMatrix
                });

            return unitInstances
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
        }

        private int GetHitGroup(ShaderBindingTable shaderTable, uint lightIndex, uint tlasIndex, ResourcePool.ResourceLifetime<GBufferKey> data, ResourcePool.ResourceLifetime<AtrousDataTextureKey> atrous, ResourcePool.ResourceLifetime<IlluminanceTextureKey> illuminance, ResourcePool.ResourceLifetime<IlluminanceTextureKey>? previousIlluminance, float ambientLight, Matrix4x4 worldMatrix, IGeometry geometry, FrameResources resources)
        {
            if (geometry is SphereGeometry sphere)
            {
                return PrepareSphereHitGroup(shaderTable, data, atrous, illuminance, resources, worldMatrix, sphere);
            }

            if (geometry is Mesh mesh)
            {
                return PrepareMeshHitGroup(shaderTable, lightIndex, tlasIndex, data, atrous, illuminance, previousIlluminance, worldMatrix, ambientLight, mesh, resources);
            }

            throw new InvalidOperationException();
        }

        private int PrepareSphereHitGroup(ShaderBindingTable shaderTable, ResourcePool.ResourceLifetime<GBufferKey> data, ResourcePool.ResourceLifetime<AtrousDataTextureKey> atrous, ResourcePool.ResourceLifetime<IlluminanceTextureKey> illuminance, FrameResources frameResources, Matrix4x4 worldMatrix, SphereGeometry sphere)
        {
            var pos = Vector3.Transform(new Vector3(0, 0, 0), worldMatrix);
            var size = Vector3.TransformNormal(new Vector3(1, 0, 0), worldMatrix).Length();
            var parameters = new Data.SphereHitGroupParameters
            {
                WorldPosition = pos,
                Size = size,
                EmissionStrength = sphere.Material.EmissionStrength,
                Colour = sphere.Material.Colour,
                EmissionColour = sphere.Material.EmissionColour,
                DataIndex = frameResources.HeapAccumulator.AddUAV(data.Resource, data.Key.UAV),
                IlluminanceTextureIndex = frameResources.HeapAccumulator.AddUAV(illuminance.Resource, illuminance.Key.UAV),
                AtrousDataTextureIndex = frameResources.HeapAccumulator.AddUAV(atrous.Resource, atrous.Key.UAV),
            }.GetBytes();

            return shaderTable.AddHit("SphereRadiance", parameters);
        }

        private int PrepareMeshHitGroup(ShaderBindingTable shaderTable, uint lightIndex, uint tlasIndex, ResourcePool.ResourceLifetime<GBufferKey> data, ResourcePool.ResourceLifetime<AtrousDataTextureKey> atrous, ResourcePool.ResourceLifetime<IlluminanceTextureKey> illuminance, ResourcePool.ResourceLifetime<IlluminanceTextureKey>? previousIlluminance, Matrix4x4 worldMatrix, float ambientLight, Mesh mesh, FrameResources resources)
        {
            var meshData = resources.Permanent.MeshResourceCache.Load(mesh, resources);

            var parameters = new Data.ObjectRadianceParameters
            {
                AmbientLight = ambientLight,
                MaxBounces = maxRays,
                Seed = rng.GetRandom<uint>(),
                LightsIndex = lightIndex,
                WorldMatrix = Matrix4x4.Transpose(worldMatrix),
                PreviousIlluminanceTextureIndex = previousIlluminance == null ? 0xFFFFFFFF : resources.HeapAccumulator.AddUAV(previousIlluminance.Resource, previousIlluminance.Key.UAV),
                TLASIndex = tlasIndex,
                TrianglesIndex = resources.HeapAccumulator.AddStructuredBuffer(meshData.Triangles),
                DataIndex = resources.HeapAccumulator.AddUAV(data.Resource, data.Key.UAV),
                IlluminanceTextureIndex = resources.HeapAccumulator.AddUAV(illuminance.Resource, illuminance.Key.UAV),
                AtrousDataTextureIndex = resources.HeapAccumulator.AddUAV(atrous.Resource, atrous.Key.UAV),
            }.GetBytes();

            return shaderTable.AddHit("ObjectRadiance", parameters);
        }

        private Shaders.Data.LightSource LightSource(Unit unit, FrameResources resources)
        {
            return LightSource(unit.Blueprint.Mesh, resources, unit.WorldMatrix);
        }

        private Shaders.Data.LightSource LightSource(PredefinedObject o, FrameResources resources)
        {
            if (o.Geometry is Mesh mesh)
            {
                return LightSource(mesh, resources, o.WorldMatrix);
            }

            if (o.Geometry is SphereGeometry sphere)
            {
                return LightSource(sphere, o.WorldMatrix);
            }

            throw new InvalidOperationException();
        }

        private Shaders.Data.LightSource LightSource(SphereGeometry sphere, Matrix4x4 worldMatrix)
        {
            var size = Vector3.TransformNormal(new Vector3(1, 0, 0), worldMatrix).Length();
            var pos = Vector3.Transform(new Vector3(0, 0, 0), worldMatrix);
            return new Data.LightSource
            {
                Size = size,
                Position = pos,
                Power = sphere.Material.EmissionStrength,
            };
        }

        private Shaders.Data.LightSource LightSource(Mesh mesh, FrameResources resources, Matrix4x4 worldMatrix)
        {
            var pos = Vector3.Transform(new Vector3(0, 0, 0), worldMatrix);
            var meshData = resources.Permanent.MeshResourceCache.Load(mesh, resources);
            return new Data.LightSource
            {
                Size = meshData.Size,
                Position = pos,
                Power = 0,
            };
        }

        private BufferView GetBLAS(string name, IGeometry geometry, FrameResources resources)
        {
            if (geometry is SphereGeometry)
            {
                return resources.Permanent.PrimitiveBlasCache.Get(resources).SphereBlas;
            }

            if (geometry is Mesh mesh)
            {
                return resources.Permanent.MeshResourceCache.Load(mesh, resources).BLAS;
            }

            throw new InvalidOperationException();
        }

        private struct InstanceDescription
        {
            public required BufferView BLAS { get; init; }
            public required Matrix4x4 Transform { get; init; }
            public required int HitGroup { get; init; }
        }
    }
}
