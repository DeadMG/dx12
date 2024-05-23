using Data;
using Wrapper;
using Wrapper.Direct3D;

namespace Renderer
{
    public class ResourceCache : IDisposable
    {
        private readonly Device device;
        private readonly CopyCommandList list;
        private readonly Dictionary<Blueprint, BlueprintData> cache = new Dictionary<Blueprint, BlueprintData>();

        public ResourceCache(Device device, CopyCommandList list)
        {
            this.device = device;
            this.list = list;
        }

        public FenceWait Load(Blueprint[] blueprints)
        {
            foreach (var blueprint in blueprints)
            {
                var vertexBuffer = device.CreateStaticBuffer(blueprint.Mesh.Vertices.SizeOf());
                var indexBuffer = device.CreateStaticBuffer(blueprint.Mesh.Indices.SizeOf());

                cache[blueprint] = new BlueprintData
                {
                    VertexBuffer = vertexBuffer,
                    IndexBuffer = indexBuffer
                };

                list.UploadData(vertexBuffer, blueprint.Mesh.Vertices);
                list.UploadData(indexBuffer, blueprint.Mesh.Indices);
            }

            return list.Execute();
        }

        public BlueprintData For(Blueprint blueprint)
        {
            return cache[blueprint];
        }

        public void Dispose()
        {
            foreach (var data in cache.Values)
            {
                data.VertexBuffer.Dispose();
                data.IndexBuffer.Dispose();
            }
        }

        public class BlueprintData
        {
            public required Resource VertexBuffer { get; init; }
            public required Resource IndexBuffer { get; init; }
        }
    }
}
