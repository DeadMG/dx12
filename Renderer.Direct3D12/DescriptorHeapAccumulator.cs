using Util;

namespace Renderer.Direct3D12
{
    internal class DescriptorHeapAccumulator : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12Device5 device;

        private readonly BufferHeap cbvUavSrvHeap;
        private readonly RenderTargetHeap renderTargetHeap;

        public DescriptorHeapAccumulator(Vortice.Direct3D12.ID3D12Device5 device)
        {
            this.device = device;

            cbvUavSrvHeap = disposeTracker.Track(new BufferHeap(device, new Vortice.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = 100000,
                Flags = Vortice.Direct3D12.DescriptorHeapFlags.ShaderVisible,
                NodeMask = 0,
                Type = Vortice.Direct3D12.DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
            }));

            renderTargetHeap = disposeTracker.Track(new RenderTargetHeap(device, new Vortice.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = 10000,
                Flags = Vortice.Direct3D12.DescriptorHeapFlags.None,
                NodeMask = 0,
                Type = Vortice.Direct3D12.DescriptorHeapType.RenderTargetView
            }));
        }

        public uint AddStructuredBuffer(StructuredBuffer buffer) =>
            cbvUavSrvHeap.AddStructuredBuffer(buffer).Index;

        public uint AddUAV(Vortice.Direct3D12.ID3D12Resource buffer, Vortice.Direct3D12.UnorderedAccessViewDescription desc) =>
            cbvUavSrvHeap.AddUAV(buffer, desc).Index;

        public uint AddRaytracingStructure(Vortice.Direct3D12.ID3D12Resource resource) =>
            cbvUavSrvHeap.AddRaytracingStructure(resource).Index;

        public Vortice.Direct3D12.CpuDescriptorHandle AddRenderTargetView(Vortice.Direct3D12.ID3D12Resource renderTarget) =>
            renderTargetHeap.AddRenderTargetView(renderTarget).Handle;

        public void Reset()
        {
            cbvUavSrvHeap.Reset();
            renderTargetHeap.Reset();
        }

        public Vortice.Direct3D12.ID3D12DescriptorHeap[] GetHeaps()
        {
            return [cbvUavSrvHeap.Heap];
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        private class DescriptorHeapHolder : IDisposable
        {
            private readonly DisposeTracker disposeTracker = new DisposeTracker();
            private readonly Vortice.Direct3D12.ID3D12DescriptorHeap heap;
            private readonly int increment;
            protected readonly Vortice.Direct3D12.ID3D12Device5 device;

            private uint start;

            public DescriptorHeapHolder(string name, Vortice.Direct3D12.ID3D12Device5 device, Vortice.Direct3D12.DescriptorHeapDescription desc)
            {
                this.device = device;

                heap = disposeTracker.Track(device.CreateDescriptorHeap(desc)).Name(name);
                increment = device.GetDescriptorHandleIncrementSize(desc.Type);

                start = 0;
            }

            public virtual void Reset()
            {
                start = 0;
            }

            public void Dispose()
            {
                disposeTracker.Track(heap);
            }

            protected DescriptorHeapSlot GetSlot()
            {
                var slot = start++;
                return new DescriptorHeapSlot
                {
                    Index = slot,
                    Handle = heap.GetCPUDescriptorHandleForHeapStart().Offset((int)slot, increment),
                };
            }

            public Vortice.Direct3D12.ID3D12DescriptorHeap Heap => heap;
        }

        private class BufferHeap : DescriptorHeapHolder
        {
            private readonly Dictionary<Vortice.Direct3D12.ID3D12Resource, DescriptorHeapSlot> raytracingStructures = new Dictionary<Vortice.Direct3D12.ID3D12Resource, DescriptorHeapSlot>();
            private readonly Dictionary<Vortice.Direct3D12.ID3D12Resource, DescriptorHeapSlot> uavs = new Dictionary<Vortice.Direct3D12.ID3D12Resource, DescriptorHeapSlot>();
            private readonly Dictionary<StructuredBuffer, DescriptorHeapSlot> structuredBuffers = new Dictionary<StructuredBuffer, DescriptorHeapSlot>();

            public BufferHeap(Vortice.Direct3D12.ID3D12Device5 device, Vortice.Direct3D12.DescriptorHeapDescription desc) : base("Accumulator CBV/UAV/SRV heap", device, desc)
            {
            }

            public override void Reset()
            {
                base.Reset();
                raytracingStructures.Clear();
                uavs.Clear();
                structuredBuffers.Clear();
            }

            public DescriptorHeapSlot AddStructuredBuffer(StructuredBuffer buffer)
            {
                if (structuredBuffers.ContainsKey(buffer)) return structuredBuffers[buffer];

                var slot = GetSlot();

                device.CreateShaderResourceView(buffer.Buffer,
                    new Vortice.Direct3D12.ShaderResourceViewDescription
                    {
                        Buffer = buffer.SRV,
                        ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Buffer,
                        Shader4ComponentMapping = Vortice.Direct3D12.ShaderComponentMapping.Default,
                    },
                    slot.Handle);

                structuredBuffers[buffer] = slot;
                return slot;
            }

            public DescriptorHeapSlot AddUAV(Vortice.Direct3D12.ID3D12Resource buffer, Vortice.Direct3D12.UnorderedAccessViewDescription desc)
            {
                if (uavs.ContainsKey(buffer)) return uavs[buffer];

                var slot = GetSlot();
                
                device.CreateUnorderedAccessView(buffer, null, desc, slot.Handle);

                uavs[buffer] = slot;

                return slot;
            }

            public DescriptorHeapSlot AddRaytracingStructure(Vortice.Direct3D12.ID3D12Resource resource)
            {
                if (raytracingStructures.ContainsKey(resource)) return raytracingStructures[resource];

                var slot = GetSlot();

                device.CreateShaderResourceView(null,
                    new Vortice.Direct3D12.ShaderResourceViewDescription
                    {
                        Format = Vortice.DXGI.Format.Unknown,
                        ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.RaytracingAccelerationStructure,
                        Shader4ComponentMapping = Vortice.Direct3D12.ShaderComponentMapping.Default,
                        RaytracingAccelerationStructure = new Vortice.Direct3D12.RaytracingAccelerationStructureShaderResourceView
                        {
                            Location = resource.GPUVirtualAddress,
                        }
                    },
                    slot.Handle);

                raytracingStructures[resource] = slot;

                return slot;
            }
        }

        private class RenderTargetHeap : DescriptorHeapHolder
        {
            private readonly Dictionary<Vortice.Direct3D12.ID3D12Resource, DescriptorHeapSlot> renderTargets = new Dictionary<Vortice.Direct3D12.ID3D12Resource, DescriptorHeapSlot>();

            public RenderTargetHeap(Vortice.Direct3D12.ID3D12Device5 device, Vortice.Direct3D12.DescriptorHeapDescription desc) : base("Accumulator Render target heap", device, desc)
            {
            }

            public override void Reset()
            {
                base.Reset();
                renderTargets.Clear();
            }
            
            public DescriptorHeapSlot AddRenderTargetView(Vortice.Direct3D12.ID3D12Resource renderTarget)
            {
                if (renderTargets.ContainsKey(renderTarget)) return renderTargets[renderTarget];

                var slot = GetSlot();
                renderTargets[renderTarget] = slot;
                device.CreateRenderTargetView(renderTarget, null, slot.Handle);
                return slot;
            }
        }

        private struct DescriptorHeapSlot
        {
            public uint Index;
            public Vortice.Direct3D12.CpuDescriptorHandle Handle;
        }
    }
}
