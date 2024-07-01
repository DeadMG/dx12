namespace Renderer.Direct3D12.Shaders.Emit
{
    public class DXIL
    {
        public string GenerateLibrary(CompilationResult result, Dictionary<string, string> typeLookup)
        {
            var constant = result.Inputs.SingleOrDefault(x => x.BindType == BindType.ConstantBuffer);
            var path = Path.GetDirectoryName(result.Path).Split(Path.DirectorySeparatorChar);
            var name = Path.GetFileNameWithoutExtension(result.Path);

            return $@"using Util;
using System.Runtime.InteropServices;

namespace Renderer.Direct3D12.{String.Join(".", path)} {{
    internal class {name} : ILibrary {{
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12RootSignature signature;

        public {name}(Vortice.Direct3D12.ID3D12Device5 device) {{
            var parameters = new List<Vortice.Direct3D12.RootParameter1>();
            parameters.Add(new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootDescriptorTable1(), Vortice.Direct3D12.ShaderVisibility.All));
            {ConstantParameter(constant, typeLookup)}

            var flags = Vortice.Direct3D12.RootSignatureFlags.LocalRootSignature | Vortice.Direct3D12.RootSignatureFlags.ConstantBufferViewShaderResourceViewUnorderedAccessViewHeapDirectlyIndexed;
            signature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(flags, parameters.ToArray()))).Name(""{String.Join(".", path)}.{name} signature"");
        }}

        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects()
        {{
            var signatureSubobject = new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.LocalRootSignature(signature));

            return [
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.DxilLibraryDescription(dxil,
                     new Vortice.Direct3D12.ExportDescription(""{result.Export}""))),
                signatureSubobject,
                new Vortice.Direct3D12.StateSubObject(new Vortice.Direct3D12.SubObjectToExportsAssociation(signatureSubobject, ""{result.Export}"")),
            ];
        }}

        public string Export => ""{result.Export}"";

        public void Dispose()
        {{
            disposeTracker.Dispose();
        }}

        private static readonly byte[] dxil = [{String.Join(",", result.DXIL.Select(s => s.ToString("0")))}];
    }}
}}";
        }

        private string ConstantParameter(BoundInput? constant, Dictionary<string, string> typeLookup)
        {
            if (constant == null) return "// no constant parameter";
            return $"parameters.Add(new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<Shaders.Data.{typeLookup[constant.Type.Name]}>() / 4), Vortice.Direct3D12.ShaderVisibility.All));";
        }
    }
}
