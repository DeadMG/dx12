using Data.Space;
using Simulation;
using System.Numerics;
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

            var categories = map.StarCategories.OrderBy(x => x.Cutoff).Select(s => new Shaders.Data.StarCategory { Colour = s.Colour, Cutoff = s.Cutoff }).ToArray();
            var categoryBuffer = disposeTracker.Track(device.CreateStaticBuffer(categories.SizeOf())).Name($"{map.Name} category buffer");
            list.UploadData(categoryBuffer, categories);

            var mapData = new MapData
            {
                CategoryBuffer = categoryBuffer,
                CategorySRV = new Vortice.Direct3D12.BufferShaderResourceView
                {
                    NumElements = categories.Length,
                    StructureByteStride = Marshal.SizeOf<Shaders.Data.StarCategory>(),
                },
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

    public class MapData
    {
        public required uint Seed { get; init; }
        public required Vortice.Direct3D12.ID3D12Resource CategoryBuffer { get; init; }
        public required Vortice.Direct3D12.BufferShaderResourceView CategorySRV { get; init; }
    }
}
