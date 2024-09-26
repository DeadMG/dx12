using Simulation;

namespace Renderer.Direct3D12
{
    internal class MapResourceCache
    {
        private readonly Dictionary<Guid, MapData> cache = new Dictionary<Guid, MapData>();

        public MapData Get(Map map, FrameResources frameResources)
        {
            if (cache.ContainsKey(map.Id)) return cache[map.Id];

            var categories = map.StarCategories.OrderBy(x => x.Cutoff).Select(s => new Shaders.Data.StarCategory { Colour = s.Colour, Cutoff = s.Cutoff }).ToArray();
            var categoryBuffer = frameResources.Permanent.UploadReadonly(frameResources.UploadBufferPool, categories);

            var mapData = new MapData
            {
                Categories = categoryBuffer,
                Seed = map.StarfieldSeed ?? (uint)Random.Shared.Next()
            };

            cache[map.Id] = mapData;

            return mapData;
        }
    }

    internal class MapData
    {
        public required uint Seed { get; init; }
        public required BufferView Categories { get; init; }
    }
}
