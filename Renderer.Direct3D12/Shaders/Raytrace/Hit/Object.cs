using Data.Space;
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

        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly RandomNumberGenerator rng;

        private readonly Vortice.Direct3D12.ID3D12RootSignature signature;
        private readonly MeshResourceCache meshResourceCache;
        private readonly uint maxRays;

        public Object(Vortice.Direct3D12.ID3D12Device5 device, MeshResourceCache meshResourceCache, uint maxRays)
        {
            this.meshResourceCache = meshResourceCache;
            this.maxRays = maxRays;
            this.rng = disposeTracker.Track(RandomNumberGenerator.Create());

            var verticesParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 0 }, Vortice.Direct3D12.ShaderVisibility.All);
            var vertexIndicesParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 1 }, Vortice.Direct3D12.ShaderVisibility.All);
            var materialIndices = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 2 }, Vortice.Direct3D12.ShaderVisibility.All);
            var materials = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 3 }, Vortice.Direct3D12.ShaderVisibility.All);
            var tlasParameter = new Vortice.Direct3D12.RootParameter1(Vortice.Direct3D12.RootParameterType.ShaderResourceView, new Vortice.Direct3D12.RootDescriptor1 { ShaderRegister = 4 }, Vortice.Direct3D12.ShaderVisibility.All);
            var lightParameter = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<Settings>() / 4), Vortice.Direct3D12.ShaderVisibility.All);

            signature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [verticesParameter, vertexIndicesParameter, materialIndices, materials, tlasParameter, lightParameter]))).Name("Object hit signature");
        }

        public string[] Exports => ["ClosestObjectHit"];

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects()
        {
            var signatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(signature));

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
            ];
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public void PrepareRaytracing(RaytracePreparation preparation)
        {
            var blueprintHitGroups = new Dictionary<Blueprint, int>();
            foreach (var unit in preparation.Volume.Units)
            {
                var seed = rng.GetRandom<uint>();

                var data = meshResourceCache.Load(unit.Blueprint.Name, unit.Blueprint.Mesh, preparation.List);

                if (!blueprintHitGroups.ContainsKey(unit.Blueprint))
                {
                    var parameters = (Vortice.Direct3D12.ID3D12Resource tlas) => BitConverter.GetBytes(data.VertexBuffer.GPUVirtualAddress)
                        .Concat(BitConverter.GetBytes(data.VertexIndexBuffer.GPUVirtualAddress))
                        .Concat(BitConverter.GetBytes(data.MaterialIndexBuffer.GPUVirtualAddress))
                        .Concat(BitConverter.GetBytes(data.MaterialBuffer.GPUVirtualAddress))
                        .Concat(BitConverter.GetBytes(tlas.GPUVirtualAddress))
                        .Concat(new Settings { MaxRays = maxRays, Seed = seed }.GetBytes())
                        .ToArray();

                    blueprintHitGroups[unit.Blueprint] = preparation.ShaderTable.AddHit("ObjectHitGroup", parameters);
                }

                preparation.InstanceDescriptions.Add(new Vortice.Direct3D12.RaytracingInstanceDescription
                {
                    AccelerationStructure = data.BLAS.GPUVirtualAddress,
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

                var data = meshResourceCache.Load(predefined.Name, predefined.Mesh, preparation.List);

                var parameters = (Vortice.Direct3D12.ID3D12Resource tlas) => BitConverter.GetBytes(data.VertexBuffer.GPUVirtualAddress)
                    .Concat(BitConverter.GetBytes(data.VertexIndexBuffer.GPUVirtualAddress))
                    .Concat(BitConverter.GetBytes(data.MaterialIndexBuffer.GPUVirtualAddress))
                    .Concat(BitConverter.GetBytes(data.MaterialBuffer.GPUVirtualAddress))
                    .Concat(BitConverter.GetBytes(tlas.GPUVirtualAddress))
                    .Concat(new Settings { MaxRays = maxRays, Seed = seed }.GetBytes())
                    .ToArray();

                var hitGroup = preparation.ShaderTable.AddHit("ObjectHitGroup", parameters);

                preparation.InstanceDescriptions.Add(new Vortice.Direct3D12.RaytracingInstanceDescription
                {
                    AccelerationStructure = data.BLAS.GPUVirtualAddress,
                    InstanceID = new Vortice.UInt24(0),
                    Flags = Vortice.Direct3D12.RaytracingInstanceFlags.ForceOpaque,
                    Transform = (Matrix4x4.CreateScale(predefined.Size) * Matrix4x4.CreateTranslation(predefined.Position)).AsAffine(),
                    InstanceMask = 0xFF,
                    InstanceContributionToHitGroupIndex = new Vortice.UInt24((uint)hitGroup)
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
        private struct Settings
        {
            [FieldOffset(0)]
            public uint Seed;

            [FieldOffset(4)]
            public uint MaxRays;
        }
    }
}
