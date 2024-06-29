using Data.Space;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12.Shaders.Raytrace.Miss
{
    internal class Starfield : IShader
    {
        private readonly ReadOnlyMemory<byte> dxil = Shader.LoadDxil("Shaders/Raytrace/Miss/Starfield.hlsl", "lib_6_3");
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12RootSignature signature;
        private readonly MapResourceCache mapResourceCache;

        public Starfield(Vortice.Direct3D12.ID3D12Device5 device)
        {
            var categoryParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 0 }, Vortice.Direct3D12.ShaderVisibility.All);
            var noiseParameter = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<HlslStarNoiseParameters>() / 4), Vortice.Direct3D12.ShaderVisibility.All);

            signature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [noiseParameter, categoryParameter])).Name("Starfield signature"));

            mapResourceCache = disposeTracker.Track(new MapResourceCache(device));
        }

        public string[] Exports => ["Miss"];

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects()
        {
            var signatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(signature));

            return [
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(dxil,
                     Exports.Select(x => new Vortice.Direct3D12.ExportDescription(x)).ToArray())),
                signatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(signatureSubobject, Exports))
            ];
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public void PrepareRaytracing(RaytracePreparation preparation)
        {
            var mapData = mapResourceCache.Get(preparation.Volume.Map, preparation.List);

            var parameters = new HlslStarNoiseParameters 
            { 
                NoiseScale = preparation.Volume.Map.StarfieldNoiseScale,
                NoiseCutoff = preparation.Volume.Map.StarfieldNoiseCutoff,
                TemperatureScale = preparation.Volume.Map.StarfieldTemperatureScale,
                StarCategories = (uint)preparation.Volume.Map.StarCategories.Length,
                Seed = mapData.Seed,
                AmbientLight = preparation.Volume.Map.AmbientLightLevel
            };

            preparation.ShaderTable.AddMiss("Miss", tlas => parameters.GetBytes().Concat(BitConverter.GetBytes(mapData.LightBuffer.GPUVirtualAddress)).ToArray());
        }

        public void FinaliseRaytracing(RaytraceFinalisation finalise)
        {
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
        }

        [StructLayout(LayoutKind.Explicit)]
        struct HlslStarNoiseParameters
        {
            [FieldOffset(0)]
            public float NoiseScale;

            [FieldOffset(4)]
            public float NoiseCutoff;

            [FieldOffset(8)]
            public float TemperatureScale;

            [FieldOffset(12)]
            public uint StarCategories;

            [FieldOffset(16)]
            public uint Seed;

            [FieldOffset(20)]
            public float AmbientLight;
        }
    }
}
