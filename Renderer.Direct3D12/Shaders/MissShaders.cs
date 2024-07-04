using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12.Shaders
{
    internal class MissShaders : IRaytracingPipelineStep
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly MapResourceCache mapResourceCache;
        private readonly Raytrace.Miss.RadianceMiss radianceMiss;
        private readonly Raytrace.Miss.ShadowMiss shadowMiss;

        public MissShaders(MapResourceCache mapResourceCache, Raytrace.Miss.RadianceMiss radianceMiss, Raytrace.Miss.ShadowMiss shadowMiss)
        {
            this.mapResourceCache = mapResourceCache;
            this.radianceMiss = radianceMiss;
            this.shadowMiss = shadowMiss;
        }

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects() => [];

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public void PrepareRaytracing(RaytracePreparation preparation)
        {
            var mapData = mapResourceCache.Get(preparation.Volume.Map, preparation.List);

            var parameters = new Data.StarfieldParameters 
            { 
                NoiseScale = preparation.Volume.Map.StarfieldNoiseScale,
                NoiseCutoff = preparation.Volume.Map.StarfieldNoiseCutoff,
                TemperatureScale = preparation.Volume.Map.StarfieldTemperatureScale,
                StarCategories = (uint)preparation.Volume.Map.StarCategories.Length,
                Seed = mapData.Seed,
                AmbientLight = preparation.Volume.Map.AmbientLightLevel,
                CategoryIndex = preparation.HeapAccumulator.AddStructuredBuffer(mapData.CategoryBuffer, 
                    new Vortice.Direct3D12.BufferShaderResourceView { NumElements = preparation.Volume.Map.StarCategories.Length, StructureByteStride = Marshal.SizeOf<Data.StarCategory>() })
            };

            preparation.ShaderTable.AddMiss(radianceMiss.Export, tlas => parameters.GetBytes());
            preparation.ShaderTable.AddMiss(shadowMiss.Export, tlas => parameters.GetBytes());
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
        }
    }
}
