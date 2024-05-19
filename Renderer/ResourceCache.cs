using Data;
using Wrapper.Direct3D;

namespace Renderer
{
    public class ResourceCache : IDisposable
    {
        private readonly CopyCommandQueue queue;
        private readonly Dictionary<Blueprint, BlueprintData> cache = new Dictionary<Blueprint, BlueprintData>();

        public ResourceCache(Device device)
        {
            queue = device.CreateCopyCommandQueue();
        }

        public void Load(Blueprint[] blueprints)
        {
            var list = queue.CreateCommandList();

            foreach (var blueprint in blueprints)
            {
                var vertexBuffer = list.CreateResource(blueprint.Mesh.Vertices);
                var indexBuffer = list.CreateResource(blueprint.Mesh.Indices);

                cache[blueprint] = new BlueprintData
                {
                    VertexBuffer = vertexBuffer,
                    IndexBuffer = indexBuffer
                };
            }

            list.Execute();
        }

        public BlueprintData For(Blueprint blueprint)
        {
            return cache[blueprint];
        }

        public FenceWait Flush()
        {
            return queue.Flush();
        }

        public void Dispose()
        {
            foreach (var data in cache.Values)
            {
                data.VertexBuffer.Dispose();
                data.IndexBuffer.Dispose();
            }

            queue.Dispose();
        }

        public class BlueprintData
        {
            public required Resource VertexBuffer { get; init; }
            public required Resource IndexBuffer { get; init; }
        }
    }
}
