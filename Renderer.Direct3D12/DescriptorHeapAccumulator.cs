using Util;

namespace Renderer.Direct3D12
{
    internal class DescriptorHeapAccumulator : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12Device5 device;

        private readonly DescriptorHeapHolder cbvUavSrvHeap;
        private readonly DescriptorHeapHolder renderTargetHeap;

        public DescriptorHeapAccumulator(Vortice.Direct3D12.ID3D12Device5 device)
        {
            this.device = device;

            cbvUavSrvHeap = disposeTracker.Track(new DescriptorHeapHolder("Accumulator CBV/UAV/SRV heap", device, new Vortice.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = 100000,
                Flags = Vortice.Direct3D12.DescriptorHeapFlags.ShaderVisible,
                NodeMask = 0,
                Type = Vortice.Direct3D12.DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
            }));

            renderTargetHeap = disposeTracker.Track(new DescriptorHeapHolder("Accumulator Render target heap", device, new Vortice.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = 10000,
                Flags = Vortice.Direct3D12.DescriptorHeapFlags.None,
                NodeMask = 0,
                Type = Vortice.Direct3D12.DescriptorHeapType.RenderTargetView
            }));
        }

        public int AddUAV(Vortice.Direct3D12.ID3D12Resource buffer)
        {
            var slot = cbvUavSrvHeap.GetSlot();

            device.CreateUnorderedAccessView(buffer,
                null,
                new Vortice.Direct3D12.UnorderedAccessViewDescription
                {
                    ViewDimension = Vortice.Direct3D12.UnorderedAccessViewDimension.Texture2D
                },
                slot.Handle);

            return slot.Index;
        }

        public int AddStructuredBuffer(Vortice.Direct3D12.ID3D12Resource buffer, Vortice.Direct3D12.BufferShaderResourceView view)
        {
            var slot = cbvUavSrvHeap.GetSlot();

            device.CreateShaderResourceView(buffer, 
                new Vortice.Direct3D12.ShaderResourceViewDescription 
                { 
                    Buffer = view
                },
                slot.Handle);

            return slot.Index;
        }

        public int AddRaytracingStructure(Vortice.Direct3D12.ID3D12Resource resource)
        {
            var slot = cbvUavSrvHeap.GetSlot();

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

            return slot.Index;
        }

        public Vortice.Direct3D12.CpuDescriptorHandle AddRenderTargetView(Vortice.Direct3D12.ID3D12Resource renderTarget)
        {
            var slot = renderTargetHeap.GetSlot();
            device.CreateShaderResourceView(renderTarget, null, slot.Handle);
            return slot.Handle;
        }

        public void Reset()
        {
            cbvUavSrvHeap.Reset();
            renderTargetHeap.Reset();
        }

        public Vortice.Direct3D12.ID3D12DescriptorHeap[] GetHeaps()
        {
            return [cbvUavSrvHeap.Heap, renderTargetHeap.Heap];
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
            
            private int start;

            public DescriptorHeapHolder(string name, Vortice.Direct3D12.ID3D12Device5 device, Vortice.Direct3D12.DescriptorHeapDescription desc)
            {
                heap = disposeTracker.Track(device.CreateDescriptorHeap(desc)).Name(name);
                increment = device.GetDescriptorHandleIncrementSize(desc.Type);

                start = 0;
            }

            public DescriptorHeapSlot GetSlot()
            {
                var slot = start++;
                return new DescriptorHeapSlot
                {
                    Index = slot,
                    Handle = heap.GetCPUDescriptorHandleForHeapStart().Offset(slot, increment),
                };
            }

            public void Reset()
            {
                start = 0;
            }

            public void Dispose()
            {
                disposeTracker.Track(heap);
            }

            public Vortice.Direct3D12.ID3D12DescriptorHeap Heap => heap;
        }

        private struct DescriptorHeapSlot
        {
            public int Index;
            public Vortice.Direct3D12.CpuDescriptorHandle Handle;
        }
    }
}
