namespace Renderer.Direct3D12.Shaders.Emit
{
    internal class Compute
    {
        public string GenerateShader(CompilationResult result, Dictionary<string, string> typeLookup)
        {
            var constant = result.Inputs.SingleOrDefault(x => x.BindType == BindType.ConstantBuffer);
            var path = Path.GetDirectoryName(result.Path).Split(Path.DirectorySeparatorChar);
            var name = Path.GetFileNameWithoutExtension(result.Path);

            return @$"using Util;
using System.Runtime.InteropServices;

namespace Renderer.Direct3D12.{String.Join(".", path)} {{
    internal class {name} : IComputeShader {{
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12PipelineState pipelineState;
        private readonly Vortice.Direct3D12.ID3D12RootSignature signature;

        public {name}(Vortice.Direct3D12.ID3D12Device5 device) 
        {{
            var parameters = new List<Vortice.Direct3D12.RootParameter1>();
            {ConstantParameter(constant, typeLookup)}

            signature = disposeTracker.Track(device.CreateRootSignature(new Vortice.Direct3D12.RootSignatureDescription1(Vortice.Direct3D12.RootSignatureFlags.ConstantBufferViewShaderResourceViewUnorderedAccessViewHeapDirectlyIndexed, parameters.ToArray()))).Name(""{String.Join(".", path)}.{name} signature"");
            pipelineState = disposeTracker.Track(device.CreateComputePipelineState(new Vortice.Direct3D12.ComputePipelineStateDescription
            {{
                ComputeShader = dxil,
                RootSignature = signature,
                Flags = Vortice.Direct3D12.PipelineStateFlags.None,
                NodeMask = 0
            }}));
        }}

        public Vortice.Direct3D12.ID3D12PipelineState PipelineState => pipelineState;
        public Vortice.Direct3D12.ID3D12RootSignature RootSignature => signature;

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
