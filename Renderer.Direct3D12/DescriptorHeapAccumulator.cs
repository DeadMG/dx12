using Util;

namespace Renderer.Direct3D12
{
    internal class DescriptorHeapAccumulator : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12Device10 device;

        private readonly BufferHeap cbvUavSrvHeap;
        private readonly RenderTargetHeap renderTargetHeap;

        public DescriptorHeapAccumulator(Vortice.Direct3D12.ID3D12Device10 device)
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

        public uint AddStructuredBuffer(BufferView buffer) =>
            cbvUavSrvHeap.AddStructuredBuffer(buffer).Index;

        public uint AddUAV(Vortice.Direct3D12.ID3D12Resource buffer, Vortice.Direct3D12.UnorderedAccessViewDescription desc) =>
            cbvUavSrvHeap.AddUAV(buffer, desc).Index;

        public TLASReservation ReserveRaytracingStructure() =>
            cbvUavSrvHeap.ReserveRaytracingStructure();

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
            private readonly uint increment;
            protected readonly Vortice.Direct3D12.ID3D12Device10 device;

            private uint start;

            public DescriptorHeapHolder(string name, Vortice.Direct3D12.ID3D12Device10 device, Vortice.Direct3D12.DescriptorHeapDescription desc)
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
            private readonly Dictionary<Vortice.Direct3D12.ID3D12Resource, DescriptorHeapSlot> uavs = new Dictionary<Vortice.Direct3D12.ID3D12Resource, DescriptorHeapSlot>();
            private readonly Dictionary<BufferView, DescriptorHeapSlot> structuredBuffers = new Dictionary<BufferView, DescriptorHeapSlot>();

            public BufferHeap(Vortice.Direct3D12.ID3D12Device10 device, Vortice.Direct3D12.DescriptorHeapDescription desc) : base("Accumulator CBV/UAV/SRV heap", device, desc)
            {
            }

            public override void Reset()
            {
                base.Reset();
                uavs.Clear();
                structuredBuffers.Clear();
            }

            public DescriptorHeapSlot AddStructuredBuffer(BufferView buffer)
            {
                if (structuredBuffers.ContainsKey(buffer)) return structuredBuffers[buffer];

                var slot = GetSlot();

                if (buffer.NumElements == 0)
                {
                    device.CreateShaderResourceView(null,
                        new Vortice.Direct3D12.ShaderResourceViewDescription
                        {
                            Buffer = buffer.SRV,
                            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Buffer,
                            Shader4ComponentMapping = Vortice.Direct3D12.ShaderComponentMapping.Default,
                        },
                        slot.Handle);
                }
                else
                {
                    device.CreateShaderResourceView(buffer.Resource,
                        new Vortice.Direct3D12.ShaderResourceViewDescription
                        {
                            Buffer = buffer.SRV,
                            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Buffer,
                            Shader4ComponentMapping = Vortice.Direct3D12.ShaderComponentMapping.Default,
                        },
                        slot.Handle);
                }

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

            public TLASReservation ReserveRaytracingStructure()
            {
                return new TLASReservation(GetSlot(), device);
            }
        }

        private class RenderTargetHeap : DescriptorHeapHolder
        {
            private readonly Dictionary<Vortice.Direct3D12.ID3D12Resource, DescriptorHeapSlot> renderTargets = new Dictionary<Vortice.Direct3D12.ID3D12Resource, DescriptorHeapSlot>();

            public RenderTargetHeap(Vortice.Direct3D12.ID3D12Device10 device, Vortice.Direct3D12.DescriptorHeapDescription desc) : base("Accumulator Render target heap", device, desc)
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

        public struct DescriptorHeapSlot
        {
            public uint Index;
            public Vortice.Direct3D12.CpuDescriptorHandle Handle;
        }

        public class TLASReservation
        {
            private readonly DescriptorHeapSlot slot;
            private readonly Vortice.Direct3D12.ID3D12Device10 device;

            public TLASReservation(DescriptorHeapSlot slot, Vortice.Direct3D12.ID3D12Device10 device)
            {
                this.slot = slot;
                this.device = device;
            }

            public uint Offset => slot.Index;

            public void Commit(BufferView TLAS)
            {
                device.CreateShaderResourceView(null,
                    new Vortice.Direct3D12.ShaderResourceViewDescription
                    {
                        ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.RaytracingAccelerationStructure,
                        Shader4ComponentMapping = Vortice.Direct3D12.ShaderComponentMapping.Default,
                        RaytracingAccelerationStructure = new Vortice.Direct3D12.RaytracingAccelerationStructureShaderResourceView
                        {
                            Location = TLAS.GPUVirtualAddress,
                        }
                    },
                    slot.Handle);
            }
        }
    }
}
