namespace Renderer.Direct3D12.Shaders.ShaderModel
{
    public class ConstantBuffer
    {
        public required string Name { get; init; }
        public required int Size { get; init; }
        public required ConstantBufferType BufferType { get; init; }
        public required bool IsUserPacked { get; init; }
        public required Variable[] Variables { get; init; }
    }

    public class Variable
    {
        public required HLSLType Type { get; init; }
        public required string Name { get; init; }
        public required int Size { get; init; }

        public required int StartSampler { get; init; }
        public required int StartTexture { get; init; }
        public required int StartOffset { get; init; }
        public required int SamplerSize { get; init; }
        public required int TextureSize { get; init; }
    }

    public enum ConstantBufferType
    {
        ConstantBuffer,
        TextureBuffer,
        ResourceBindInfo,
        InterfacePointers
    }
}
