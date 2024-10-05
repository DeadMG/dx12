namespace Renderer.Direct3D12.Graph
{
    internal class RaytraceNode : IGraphNode
    {
        public required TextureGraphResource HistoryIlluminance { get; init; }

        public required TextureGraphResource OutputIlluminance { get; init; }
        public required BufferGraphResource OutputData { get; init; }
    }
}
