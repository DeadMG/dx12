using Data.Space;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12.Shaders.Raytrace.Hit
{
    internal class Sun : IShader
    {
        private readonly ReadOnlyMemory<byte> sun = Shader.LoadDxil("Shaders/Raytrace/Hit/Sun.hlsl", "lib_6_3");
        private readonly ReadOnlyMemory<byte> sunLight = Shader.LoadDxil("Shaders/Raytrace/Hit/SunLight.hlsl", "lib_6_3");

        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12RootSignature sunSignature;
        private readonly Vortice.Direct3D12.ID3D12RootSignature sunLightSignature;
        private readonly MeshResourceCache meshResourceCache;
        private readonly IcosphereMesh sunMesh = new IcosphereGenerator().Generate(3);

        public Sun(Vortice.Direct3D12.ID3D12Device5 device, MeshResourceCache meshResourceCache)
        {
            this.meshResourceCache = meshResourceCache;

            var sunParameter = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<SunColour>() / 4), Vortice.Direct3D12.ShaderVisibility.All);
            sunSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [sunParameter])).Name("Sun signature"));

            var sunLightParameter = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<SunLight>() / 4), Vortice.Direct3D12.ShaderVisibility.All);
            sunLightSignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [sunLightParameter])).Name("Sunlight signature"));
        }

        public string[] Exports => ["ClosestSunHit", "SunLightHit"];

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects()
        {
            var sunSignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(sunSignature));
            var sunLightSignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(sunSignature));

            return [
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "SunHitGroup",
                    ClosestHitShaderImport = "ClosestSunHit",
                }),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "SunLightHitGroup",
                    ClosestHitShaderImport = "SunLightHit",
                }),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(sun,
                     new Vortice.Direct3D12.ExportDescription("ClosestSunHit"))),
                sunSignatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(sunSignatureSubobject, "ClosestSunHit")),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(sunLight,
                     new Vortice.Direct3D12.ExportDescription("SunLightHit"))),
                sunLightSignatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(sunSignatureSubobject, "SunLightHit")),
            ];
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public void PrepareRaytracing(RaytracePreparation preparation)
        {
            var sunData = meshResourceCache.Load(sunMesh.Id, "Sun", () => sunMesh.Vertices, sunMesh.Indices, preparation.List);

            foreach (var sun in preparation.Volume.Map.Suns)
            {
                var hitGroup = preparation.ShaderTable.AddHit("SunHitGroup", tlas => new SunColour { Colour = sun.MeshColour }.GetBytes());
                preparation.InstanceDescriptions.Add(new Vortice.Direct3D12.RaytracingInstanceDescription
                {
                    AccelerationStructure = sunData.BLAS.GPUVirtualAddress,
                    InstanceID = new Vortice.UInt24(0),
                    Flags = Vortice.Direct3D12.RaytracingInstanceFlags.None,
                    InstanceMask = 0xFF,
                    InstanceContributionToHitGroupIndex = new Vortice.UInt24((uint)hitGroup),
                    Transform = (Matrix4x4.CreateScale(sun.Size) * Matrix4x4.CreateTranslation(sun.Position)).AsAffine()
                });
                preparation.ShaderTable.AddHit("SunLightHitGroup", tlas => new SunLight { Colour = sun.LightColour, Intensity = sun.LightIntensity }.GetBytes());
            }
        }

        public void FinaliseRaytracing(RaytraceFinalisation finalise)
        {
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct SunColour
        {
            [FieldOffset(0)]
            public RGB Colour;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct SunLight
        {
            [FieldOffset(0)]
            public RGB Colour;

            [FieldOffset(12)]
            public float Intensity;
        }
    }
}
