using Util;

namespace Renderer.Direct3D12.Shaders.Raytrace.Miss
{
    internal class Black : IShader
    {
        private readonly ReadOnlyMemory<byte> dxil = Shader.LoadDxil("Shaders/Raytrace/Miss/Black.hlsl", "lib_6_3");
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12RootSignature signature;

        public Black(Vortice.Direct3D12.ID3D12Device5 device)
        {
            signature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature, [])).Name("Empty local signature"));
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
            preparation.ShaderTable.AddMiss("Miss", tlas => new byte[0]);
        }

        public void FinaliseRaytracing(RaytraceFinalisation finalise)
        {
        }

        public void CommitRaytracing(RaytraceCommit commit)
        {
        }
    }
}
