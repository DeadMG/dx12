using Util;

namespace Renderer.Direct3D12
{
    internal class StateObjectProperties : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Dictionary<string, byte[]> shaderIdCache = new Dictionary<string, byte[]>();
        private readonly Vortice.Direct3D12.ID3D12StateObjectProperties properties;

        public StateObjectProperties(Vortice.Direct3D12.ID3D12StateObject obj)
        {
            properties = disposeTracker.Track(obj.QueryInterface<Vortice.Direct3D12.ID3D12StateObjectProperties>());
        }

        public unsafe byte[] Get(string exportName)
        {
            if (!shaderIdCache.ContainsKey(exportName))
            {
                shaderIdCache[exportName] = new Span<byte>((void*)properties.GetShaderIdentifier(exportName), IdentifierSize).ToArray(); ;
            }

            return shaderIdCache[exportName];
        }

        public const int IdentifierSize = 32; // D3D12_RAYTRACING_SHADER_IDENTIFIER_SIZE_IN_BYTES

        public void Dispose()
        {
            disposeTracker.Dispose();
        }
    }
}
