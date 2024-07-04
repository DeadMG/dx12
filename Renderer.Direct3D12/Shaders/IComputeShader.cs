namespace Renderer.Direct3D12.Shaders
{
    internal interface IComputeShader : IDisposable
    {
        Vortice.Direct3D12.ID3D12PipelineState PipelineState { get; }
        Vortice.Direct3D12.ID3D12RootSignature RootSignature { get; }
    }
}
