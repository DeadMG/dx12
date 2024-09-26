using System.Runtime.InteropServices;

namespace Renderer.Direct3D12
{
    internal class BufferPool : IDisposable
    {
        private readonly HashSet<HeapBuffer> heapBuffers = new HashSet<HeapBuffer>();
        private readonly object syncObject = new object();

        private readonly Vortice.Direct3D12.ID3D12Device10 device;
        private readonly Vortice.Direct3D12.HeapType heapType;
        private readonly Vortice.Direct3D12.ResourceFlags flags;
        private readonly uint bufferSize;
        private readonly string name;

        public BufferPool(Vortice.Direct3D12.ID3D12Device10 device, uint bufferSize, Vortice.Direct3D12.HeapType heapType, Vortice.Direct3D12.ResourceFlags flags, string name)
        {
            this.device = device;
            this.bufferSize = bufferSize;
            this.heapType = heapType;
            this.flags = flags;
            this.name = name;
        }

        public BufferView AllocateFor<T>(ReadOnlySpan<T> items, uint? alignment = null)
        {
            return Allocate(alignment ?? (uint)Marshal.SizeOf<T>(), (uint)items.Length, (uint)Marshal.SizeOf<T>());
        }

        public BufferView Allocate<T>(uint elements, uint? alignment = null)
        {
            return Allocate(alignment ?? (uint)Marshal.SizeOf<T>(), elements, (uint)Marshal.SizeOf<T>());
        }

        public BufferView Allocate(uint alignment, uint elements, uint structureStride)
        {
            var totalSize = (elements * structureStride).Align(alignment);

            lock (syncObject)
            {
                foreach (var buffer in heapBuffers)
                {
                    var startOffset = buffer.CurrentUsage.Align(alignment);
                    var endOffset = totalSize + startOffset;

                    if (endOffset < buffer.Resource.Description.Width)
                    {
                        buffer.CurrentUsage = endOffset;
                        return new BufferView(buffer.Resource, startOffset / structureStride, elements, structureStride);
                    }
                }

                var desc = Vortice.Direct3D12.ResourceDescription1.Buffer(new Vortice.Direct3D12.ResourceAllocationInfo(Math.Max(totalSize, bufferSize), 65536), flags);
                var resource = device.CreateCommittedResource3<Vortice.Direct3D12.ID3D12Resource>(
                    new Vortice.Direct3D12.HeapProperties(heapType),
                    Vortice.Direct3D12.HeapFlags.None,
                    desc,
                    Vortice.Direct3D12.BarrierLayout.Undefined,
                    null,
                    null,
                    null).Name($"Pool {name} buffer {heapBuffers.Count}");
                heapBuffers.Add(new HeapBuffer { Resource = resource, CurrentUsage = totalSize });
                return new BufferView(resource, 0, elements, structureStride);
            }
        }

        public void Reset()
        {
            lock (syncObject)
            {
                foreach (var buffer in heapBuffers)
                {
                    buffer.CurrentUsage = 0;
                }
            }
        }

        public void Shrink()
        {
            lock (syncObject)
            {
                foreach (var buffer in heapBuffers)
                {
                    if (buffer.CurrentUsage == 0)
                    {
                        buffer.Resource.Dispose();
                    }
                    heapBuffers.Remove(buffer);
                }
            }
        }

        public void Dispose()
        {
            foreach (var buffer in heapBuffers)
            {
                buffer.Resource.Dispose();
            }
        }

        private class HeapBuffer
        {
            public required Vortice.Direct3D12.ID3D12Resource Resource { get; init; }
            public uint CurrentUsage = 0;
        }
    }
}
