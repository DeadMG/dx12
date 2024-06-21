namespace Renderer.Direct3D12.Shaders.ShaderModel
{
    public class CompilationResult
    {
        public required ConstantBuffer[] ConstantBuffers { get; init; }
        public required byte[] DXIL { get; init; }
    }
}
