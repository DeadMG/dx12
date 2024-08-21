using Simulation;
using System.Security.Cryptography;
using Util;

namespace Renderer.Direct3D12
{
    internal class MapResourceCache : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Dictionary<Guid, MapData> cache = new Dictionary<Guid, MapData>();
        private readonly Vortice.Direct3D12.ID3D12Device5 device;

        private readonly RandomNumberGenerator rng;

        public MapResourceCache(Vortice.Direct3D12.ID3D12Device5 device)
        {
            rng = disposeTracker.Track(RandomNumberGenerator.Create());
            this.device = device;
        }

        public MapData Get(Map map, PooledCommandList list)
        {
            if (cache.ContainsKey(map.Id)) return cache[map.Id];

            var categories = map.StarCategories.OrderBy(x => x.Cutoff).Select(s => new Shaders.Data.StarCategory { Colour = s.Colour, Cutoff = s.Cutoff }).ToArray();
            var categoryBuffer = disposeTracker.Track(device.CreateStaticBuffer(categories.SizeOf())).Name($"{map.Name} category buffer");
            
            var mapData = new MapData
            {
                Categories = list.UploadData(categoryBuffer, categories),
                Seed = map.StarfieldSeed ?? rng.GetRandom<uint>()
            };

            cache[map.Id] = mapData;

            return mapData;
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }
    }

    internal class MapData
    {
        public required uint Seed { get; init; }
        public required StructuredBuffer Categories { get; init; }
    }
}
