using Util;

namespace Renderer.Direct3D12
{
    public class ResourcePool : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Dictionary<ResourceKey, HashSet<Vortice.Direct3D12.ID3D12Resource>> resources = new Dictionary<ResourceKey, HashSet<Vortice.Direct3D12.ID3D12Resource>>();
        private readonly Vortice.Direct3D12.ID3D12Device5 device;

        public ResourcePool(Vortice.Direct3D12.ID3D12Device5 device)
        {
            this.device = device;
        }

        public void Dispose() => disposeTracker.Dispose();

        public Vortice.Direct3D12.ID3D12Resource GetResource(ResourceKey key, string usage)
        {
            if (!resources.ContainsKey(key)) resources[key] = new HashSet<Vortice.Direct3D12.ID3D12Resource>();
            if (resources[key].Count == 0)
            {
                return disposeTracker.Track(device.CreateCommittedResource(key.HeapType, key.Description, key.InitialState).Name(usage));
            }

            var resource = resources[key].First();
            resources[key].Remove(resource);
            return resource.Name(usage);
        }

        public ResourceLifetime<T> LeaseResource<T>(T key, string usage)
            where T : ResourceKey
        {
            return new ResourceLifetime<T> { Key = key, Pool = this, Resource = GetResource(key, usage) };
        }

        public void ReturnResource(ResourceKey key, Vortice.Direct3D12.ID3D12Resource resource)
        {
            resources[key].Add(resource);
        }

        public class ResourceKey
        {
            public required Vortice.Direct3D12.HeapType HeapType { get; init; }
            public required Vortice.Direct3D12.ResourceDescription Description { get; init; }
            public required Vortice.Direct3D12.ResourceStates InitialState { get; init; }
        }

        public class UAVResourceKey : ResourceKey
        {
            public required Vortice.Direct3D12.UnorderedAccessViewDescription UAV { get; init; }
        }

        public class ResourceLifetime<T> : IDisposable
            where T : ResourceKey
        {
            public required T Key { get; init; }
            public required ResourcePool Pool { get; init; }
            public required Vortice.Direct3D12.ID3D12Resource Resource { get; init; }

            public void Dispose() => Pool.ReturnResource(Key, Resource);
        }
    }
}
