using Simulation;
using Util;

namespace Renderer.Direct3D12
{
    public class ResourceCache : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly SharpDX.Direct3D12.Device device;
        private readonly Dictionary<Blueprint, BlueprintData> cache = new Dictionary<Blueprint, BlueprintData>();

        public ResourceCache(SharpDX.Direct3D12.Device device)
        {
            this.device = device;
        }

        public BlueprintData For(Blueprint blueprint, PooledCommandList list)
        {
            if (cache.ContainsKey(blueprint)) return cache[blueprint];

            var vertexBuffer = disposeTracker.Track(device.CreateStaticBuffer(blueprint.Mesh.Vertices.SizeOf()));
            var indexBuffer = disposeTracker.Track(device.CreateStaticBuffer(blueprint.Mesh.Indices.SizeOf()));

            cache[blueprint] = new BlueprintData
            {
                VertexBuffer = vertexBuffer,
                IndexBuffer = indexBuffer
            };

            list.UploadData(vertexBuffer, blueprint.Mesh.Vertices);
            list.UploadData(indexBuffer, blueprint.Mesh.Indices);

            return cache[blueprint];
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public class BlueprintData
        {
            public required SharpDX.Direct3D12.Resource VertexBuffer { get; init; }
            public required SharpDX.Direct3D12.Resource IndexBuffer { get; init; }
        }
    }
}
