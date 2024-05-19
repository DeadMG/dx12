using SharpDX.Mathematics.Interop;

namespace Wrapper.Direct3D
{
    public class SwapChain : IDisposable
    {
        private readonly SharpDX.Direct3D12.Device device;
        private readonly SharpDX.DXGI.SwapChain3 swapChain;
        private readonly SharpDX.Direct3D12.DescriptorHeap descriptorHeap;
        private readonly List<RenderTargetView> backBuffers = new List<RenderTargetView>();
        private readonly bool allowTearing;

        public SwapChain(SharpDX.Direct3D12.Device device, SharpDX.DXGI.SwapChain3 swapChain, bool allowTearing)
        {
            this.swapChain = swapChain;
            this.allowTearing = allowTearing;
            this.device = device;

            descriptorHeap = device.CreateDescriptorHeap(new SharpDX.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = swapChain.Description1.BufferCount,
                Flags = SharpDX.Direct3D12.DescriptorHeapFlags.None,
                NodeMask = 0,
                Type = SharpDX.Direct3D12.DescriptorHeapType.RenderTargetView
            });

            CreateBackBuffers();
        }

        public RenderTargetFormat RenderTargetFormat => new RenderTargetFormat { Format = swapChain.Description1.Format };

        private void CreateBackBuffers()
        {
            var format = swapChain.Description1.Format;
            var size = device.GetDescriptorHandleIncrementSize(SharpDX.Direct3D12.DescriptorHeapType.RenderTargetView);
            var handle = descriptorHeap.CPUDescriptorHandleForHeapStart;

            for (int i = 0; i < swapChain.Description1.BufferCount; ++i)
            {
                var backBuffer = swapChain.GetBackBuffer<SharpDX.Direct3D12.Resource>(i);
                unsafe
                {
                    device.CreateRenderTargetView(backBuffer, null, handle);
                }
                backBuffers.Add(new RenderTargetView(backBuffer, format, handle));
                handle.Ptr += size;
            }
        }

        public void Resize(int width, int height)
        {
            width = Math.Max(width, 1);
            height = Math.Max(height, 1);

            var desc = swapChain.Description1;

            if (desc.Width == width && desc.Height == height) return; // Nothing needed

            foreach (var buffer in backBuffers)
            {
                buffer.Dispose();
            }

            backBuffers.Clear();

            swapChain.ResizeBuffers(desc.BufferCount, width, height, desc.Format, desc.Flags);

            CreateBackBuffers();
        }

        public void Present(CommandList list)
        {
            list.Barrier(backBuffers[swapChain.CurrentBackBufferIndex].AsResource, SharpDX.Direct3D12.ResourceStates.RenderTarget, SharpDX.Direct3D12.ResourceStates.Present);
            list.Execute();

            if (allowTearing)
            {
                swapChain.Present(0, SharpDX.DXGI.PresentFlags.AllowTearing);
            }
            else
            {
                swapChain.Present(0, SharpDX.DXGI.PresentFlags.DoNotWait);
            }
        }

        public RenderTargetView PrepareBackBuffer(DirectCommandList list, RawColor4? clearColor) 
        {
            var currentBuffer = backBuffers[swapChain.CurrentBackBufferIndex];
            list.Barrier(currentBuffer.AsResource, SharpDX.Direct3D12.ResourceStates.Present, SharpDX.Direct3D12.ResourceStates.RenderTarget);
            if (clearColor.HasValue)
            {
                list.Clear(currentBuffer, clearColor.Value);
            }
            return currentBuffer;
        }

        public void Dispose()
        {
            foreach (var buffer in backBuffers)
            {
                buffer.Dispose();
            }
            descriptorHeap.Dispose();
            swapChain.Dispose();
        }
    }
}
