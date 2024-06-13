using Simulation;
using Util;

namespace Renderer.Direct3D12
{
    public class ResourceCache : IDisposable
    {
        private readonly VertexCalculator vertexCalculator = new VertexCalculator();
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Dictionary<Blueprint, BlueprintData> cache = new Dictionary<Blueprint, BlueprintData>();

        private readonly Vortice.Direct3D12.ID3D12Device5 device;
        private readonly bool supportsRaytracing;

        public ResourceCache(Vortice.Direct3D12.ID3D12Device5 device, bool supportsRaytracing)
        {
            this.device = device;
            this.supportsRaytracing = supportsRaytracing;
        }

        public BlueprintData For(Blueprint blueprint, PooledCommandList list)
        {
            if (cache.ContainsKey(blueprint)) return cache[blueprint];

            var verts = vertexCalculator.CalculateVertices(blueprint.Mesh);

            var data = new BlueprintData
            {
                VertexBuffer = disposeTracker.Track(device.CreateStaticBuffer(verts.SizeOf())),
                IndexBuffer = disposeTracker.Track(device.CreateStaticBuffer(blueprint.Mesh.Indices.SizeOf())),
                Raytracing = PrepareRaytracing(blueprint, list)
            };

            cache[blueprint] = data;

            list.UploadData(data.VertexBuffer, verts);
            list.UploadData(data.IndexBuffer, blueprint.Mesh.Indices);

            return cache[blueprint];
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        private RaytracingData? PrepareRaytracing(Blueprint blueprint, PooledCommandList list)
        {
            if (!supportsRaytracing) return null;

            return null;
        }

        public class BlueprintData
        {
            public required Vortice.Direct3D12.ID3D12Resource VertexBuffer { get; init; }
            public required Vortice.Direct3D12.ID3D12Resource IndexBuffer { get; init; }
            public required RaytracingData? Raytracing { get; init; }
        }

        public class RaytracingData
        {

        }
    }
}
