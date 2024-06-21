using Simulation;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Util;

namespace Renderer.Direct3D12.Shaders.Raytrace.Hit
{
    internal class Object : IShader
    {
        private readonly ReadOnlyMemory<byte> obj = Shader.LoadDxil("Shaders/Raytrace/Hit/Object.hlsl", "lib_6_3");
        private readonly ReadOnlyMemory<byte> light = Shader.LoadDxil("Shaders/Raytrace/Hit/ObjectLight.hlsl", "lib_6_3");

        private readonly VertexCalculator vertexCalculator = new VertexCalculator();
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly RandomNumberGenerator rng;

        private readonly Vortice.Direct3D12.ID3D12RootSignature emptySignature;
        private readonly Vortice.Direct3D12.ID3D12RootSignature signature;
        private readonly MeshResourceCache meshResourceCache;

        public Object(Vortice.Direct3D12.ID3D12Device5 device, MeshResourceCache meshResourceCache)
        {
            this.meshResourceCache = meshResourceCache;
            this.rng = disposeTracker.Track(RandomNumberGenerator.Create());

            var verticesParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 0 }, Vortice.Direct3D12.ShaderVisibility.All);
            var indicesParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 1 }, Vortice.Direct3D12.ShaderVisibility.All);
            var lightSourcesParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 2 }, Vortice.Direct3D12.ShaderVisibility.All);
            var tlasParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 3 }, Vortice.Direct3D12.ShaderVisibility.All);
            var lightParameter = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<Light>() / 4), Vortice.Direct3D12.ShaderVisibility.All);

            signature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [verticesParameter, indicesParameter, lightSourcesParameter, tlasParameter, lightParameter]))).Name("Object hit signature");

            emptySignature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [])).Name("Empty local signature"));
        }

        public string[] Exports => ["ClosestObjectHit", "ObjectLight"];

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects()
        {
            var signatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(signature));
            var emptySignatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(emptySignature));

            return [
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "ObjectHitGroup",
                    ClosestHitShaderImport = "ClosestObjectHit",
                }),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(obj,
                     new Vortice.Direct3D12.ExportDescription("ClosestObjectHit"))),
                signatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(signatureSubobject, "ClosestObjectHit")),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.HitGroupDescription
                {
                    Type = Vortice.Direct3D12.HitGroupType.Triangles,
                    HitGroupExport = "ObjectLightHitGroup",
                    ClosestHitShaderImport = "ObjectLight",
                }),
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(light,
                     new Vortice.Direct3D12.ExportDescription("ObjectLight"))),
                emptySignatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(emptySignatureSubobject, "ObjectLight")),
            ];
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public void PrepareRaytracing(RaytracePreparation preparation)
        {
            var seed = rng.GetRandom<uint>();
            //var phi = (seed & 0xFFFF) / (float)0xFFFF;
            //var theta = ((seed >> 16) & 0xFFFF) / (float)0xFFFF;
            //var offset = new Vector3((float)(Math.Sin(phi) * Math.Cos(theta)), (float)(Math.Sin(phi) * Math.Sin(theta)), (float)Math.Cos(phi));

            var suns = preparation.List.CreateUploadBuffer(preparation.Volume.Map.Suns.Select(x => new Sun { Target = x.Position, Size = x.Size }).ToArray());

            var blueprintHitGroups = new Dictionary<Blueprint, int>();
            foreach (var unit in preparation.Volume.Units)
            {
                var data = meshResourceCache.Load(unit.Blueprint.Mesh.Id, unit.Blueprint.Name, () => vertexCalculator.CalculateVertices(unit.Blueprint.Mesh), unit.Blueprint.Mesh.Indices, preparation.List);

                if (!blueprintHitGroups.ContainsKey(unit.Blueprint))
                {
                    var parameters = (Vortice.Direct3D12.ID3D12Resource tlas) => BitConverter.GetBytes(data.VertexBuffer.GPUVirtualAddress)
                        .Concat(BitConverter.GetBytes(data.IndexBuffer.GPUVirtualAddress))
                        .Concat(BitConverter.GetBytes(suns.GPUVirtualAddress))
                        .Concat(BitConverter.GetBytes(tlas.GPUVirtualAddress))
                        .Concat(new Light { AmbientLightLevel = preparation.Volume.Map.AmbientLightLevel, Sources = (uint)preparation.Volume.Map.Suns.Length, Seed = seed }.GetBytes())
                        .ToArray();

                    blueprintHitGroups[unit.Blueprint] = preparation.ShaderTable.AddHit("ObjectHitGroup",  parameters);
                    preparation.ShaderTable.AddHit("ObjectLightHitGroup", (tlas) => new byte[0]);
                }

                preparation.InstanceDescriptions.Add(new Vortice.Direct3D12.RaytracingInstanceDescription
                {
                    AccelerationStructure = data.BLAS.GPUVirtualAddress,
                    InstanceID = new Vortice.UInt24(0),
                    Flags = Vortice.Direct3D12.RaytracingInstanceFlags.None,
                    Transform = unit.WorldMatrix.AsAffine(),
                    InstanceMask = 0xFF,
                    InstanceContributionToHitGroupIndex = new Vortice.UInt24((uint)blueprintHitGroups[unit.Blueprint])
                });
            }
        }

        public void FinaliseRaytracing(RaytraceFinalisation finalise)
        {
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {

        }

        [StructLayout(LayoutKind.Explicit)]
        private struct Light
        {
            [FieldOffset(0)]
            public float AmbientLightLevel;

            [FieldOffset(4)]
            public uint Sources;

            [FieldOffset(8)]
            public uint Seed;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct Sun
        {
            [FieldOffset(0)]
            public Vector3 Target;

            [FieldOffset(12)]
            public float Size;
        }
    }
}
