using Data.Space;
using Simulation;
using System.Runtime.InteropServices;
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

            var categories = map.StarCategories.OrderBy(x => x.Cutoff).Select(s => new HlslStarCategory { Colour = s.Colour, Cutoff = s.Cutoff }).ToArray();
            var buffer = disposeTracker.Track(device.CreateStaticBuffer(categories.SizeOf())).Name($"{map.Name} light buffer");
            list.UploadData(buffer, categories);

            var mapData = new MapData
            {
                LightBuffer = buffer,
                Seed = map.StarfieldSeed ?? rng.GetRandom<uint>()
            };

            cache[map.Id] = mapData;

            return mapData;
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        [StructLayout(LayoutKind.Explicit)]
        struct HlslStarCategory
        {
            [FieldOffset(0)]
            public RGB Colour;

            [FieldOffset(12)]
            public float Cutoff;
        }
    }

    public class MapData
    {
        public required uint Seed { get; init; }
        public required Vortice.Direct3D12.ID3D12Resource LightBuffer { get; init; }
    }
}
