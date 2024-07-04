using Simulation;
using System.Numerics;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Util;
using static Renderer.Direct3D12.MeshResourceCache;

namespace Renderer.Direct3D12.Shaders
{
    internal class ObjectStep : IRaytracingPipelineStep
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly RandomNumberGenerator rng;
        private readonly MapResourceCache mapResourceCache;

        private readonly MeshResourceCache meshResourceCache;
        private readonly uint maxRays;
        private readonly Raytrace.Hit.ObjectRadiance objectRadiance;
        private readonly Raytrace.Hit.ObjectShadow objectShadow;
        private readonly Raytrace.Hit.SphereRadiance sphereRadiance;
        private readonly Raytrace.Hit.SphereShadow sphereShadow;
        private readonly Raytrace.Hit.SphereIntersection sphereIntersection;

        public ObjectStep(MeshResourceCache meshResourceCache, MapResourceCache mapResourceCache, uint maxRays, Raytrace.Hit.ObjectRadiance objectRadiance, Raytrace.Hit.ObjectShadow objectShadow, Raytrace.Hit.SphereRadiance sphereRadiance, Raytrace.Hit.SphereShadow sphereShadow, Raytrace.Hit.SphereIntersection sphereIntersection)
        {
            this.meshResourceCache = meshResourceCache;
            this.maxRays = maxRays;
            this.objectRadiance = objectRadiance;
            this.objectShadow = objectShadow;
            this.sphereRadiance = sphereRadiance;
            this.sphereShadow = sphereShadow;
            this.sphereIntersection = sphereIntersection;
            this.mapResourceCache = mapResourceCache;
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
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "ObjectShadow",
                    ClosestHitShaderImport = objectShadow.Export,
                }),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "SphereRadiance",
                    IntersectionShaderImport = sphereIntersection.Export,
                    ClosestHitShaderImport = sphereRadiance.Export,
                }),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "SphereShadow",
                    IntersectionShaderImport = sphereIntersection.Export,
                    ClosestHitShaderImport = sphereShadow.Export,
                }),
            ];
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public void PrepareRaytracing(RaytracePreparation preparation)
        {
            var mapData = mapResourceCache.Get(preparation.Volume.Map, preparation.List);
            var blueprintHitGroups = new Dictionary<Blueprint, int>();
            foreach (var unit in preparation.Volume.Units)
            {
                var seed = rng.GetRandom<uint>();

                var meshData = meshResourceCache.Load(unit.Blueprint.Name, unit.Blueprint.Mesh, preparation.List);

                if (!blueprintHitGroups.ContainsKey(unit.Blueprint))
                {
                    var parameters = (Vortice.Direct3D12.ID3D12Resource tlas) => new Data.ObjectRadianceParameters
                    {
                        MaxSamples = 4,
                        MaxBounces = maxRays,
                        Seed = seed,
                        LightsIndex = preparation.HeapAccumulator.AddStructuredBuffer(mapData.LightBuffer, mapData.LightSRV),
                        TLASIndex = preparation.HeapAccumulator.AddRaytracingStructure(tlas),
                        MaterialIndicesIndex = preparation.HeapAccumulator.AddStructuredBuffer(meshData.MaterialIndexBuffer, meshData.MaterialIndexSRV),
                        MaterialsIndex = preparation.HeapAccumulator.AddStructuredBuffer(meshData.MaterialBuffer, meshData.MaterialSRV),
                        VertexIndicesIndex = preparation.HeapAccumulator.AddStructuredBuffer(meshData.VertexIndexBuffer, meshData.VertexIndexSRV),
                        VerticesIndex = preparation.HeapAccumulator.AddStructuredBuffer(meshData.VertexBuffer, meshData.VertexSRV),
                        Lights = (uint)preparation.Volume.Map.PrimaryLights.Length,
                    }.GetBytes();

                    blueprintHitGroups[unit.Blueprint] = preparation.ShaderTable.AddHit("ObjectRadiance", parameters);

                    preparation.ShaderTable.AddHit("ObjectShadow", tlas => new byte[0]);
                }

                preparation.InstanceDescriptions.Add(new Vortice.Direct3D12.RaytracingInstanceDescription
                {
                    AccelerationStructure = meshData.BLAS.GPUVirtualAddress,
                    InstanceID = new Vortice.UInt24(0),
                    Flags = Vortice.Direct3D12.RaytracingInstanceFlags.ForceOpaque,
                    Transform = unit.WorldMatrix.AsAffine(),
                    InstanceMask = 0xFF,
                    InstanceContributionToHitGroupIndex = new Vortice.UInt24((uint)blueprintHitGroups[unit.Blueprint])
                });
            }

            foreach (var predefined in preparation.Volume.Map.Objects)
            {
                var seed = rng.GetRandom<uint>();

                var meshData = meshResourceCache.Load(predefined.Name, predefined.Mesh, preparation.List);

                var parameters = (Vortice.Direct3D12.ID3D12Resource tlas) => new Data.ObjectRadianceParameters
                {
                    MaxSamples = 4,
                    MaxBounces = maxRays,
                    Seed = seed,
                    LightsIndex = preparation.HeapAccumulator.AddStructuredBuffer(mapData.LightBuffer, mapData.LightSRV),
                    TLASIndex = preparation.HeapAccumulator.AddRaytracingStructure(tlas),
                    MaterialIndicesIndex = preparation.HeapAccumulator.AddStructuredBuffer(meshData.MaterialIndexBuffer, meshData.MaterialIndexSRV),
                    MaterialsIndex = preparation.HeapAccumulator.AddStructuredBuffer(meshData.MaterialBuffer, meshData.MaterialSRV),
                    VertexIndicesIndex = preparation.HeapAccumulator.AddStructuredBuffer(meshData.VertexIndexBuffer, meshData.VertexIndexSRV),
                    VerticesIndex = preparation.HeapAccumulator.AddStructuredBuffer(meshData.VertexBuffer, meshData.VertexSRV),
                    Lights = (uint)preparation.Volume.Map.PrimaryLights.Length,
                }.GetBytes();

                var hitGroup = preparation.ShaderTable.AddHit("ObjectRadiance", parameters);
                preparation.ShaderTable.AddHit("ObjectShadow", tlas => new byte[0]);

                preparation.InstanceDescriptions.Add(new Vortice.Direct3D12.RaytracingInstanceDescription
                {
                    AccelerationStructure = meshData.BLAS.GPUVirtualAddress,
                    InstanceID = new Vortice.UInt24(0),
                    Flags = Vortice.Direct3D12.RaytracingInstanceFlags.ForceOpaque,
                    Transform = (Matrix4x4.CreateScale(predefined.Size) * Matrix4x4.CreateTranslation(predefined.Position)).AsAffine(),
                    InstanceMask = 0xFF,
                    InstanceContributionToHitGroupIndex = new Vortice.UInt24((uint)hitGroup)
                });
            }

            foreach (var light in preparation.Volume.Map.PrimaryLights)
            {
                var parameters = (Vortice.Direct3D12.ID3D12Resource tlas) => new Data.SphereHitGroupParameters
                {
                    Size = light.Size,
                    EmissionStrength = light.Material.EmissionStrength,
                    Colour = light.Material.Colour,
                    EmissionColour = light.Material.EmissionColour,
                    WorldPosition = light.Position,
                }.GetBytes();
                var hitGroup = preparation.ShaderTable.AddHit("SphereRadiance", parameters);
                preparation.ShaderTable.AddHit("SphereShadow", parameters);
            }
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
        }
    }
}
