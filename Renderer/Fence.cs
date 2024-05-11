using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Renderer
{
    class Fence : IDisposable
    {
        private readonly ComPtr<ID3D12Fence> fence;
        private readonly ComPtr<ID3D12CommandQueue> commandQueue;

        private ulong value = 0;

        public Fence(ComPtr<ID3D12Device> device, ComPtr<ID3D12CommandQueue> commandQueue)
        {
            this.commandQueue = commandQueue;
            fence = device.CreateFence<ID3D12Fence>(value, FenceFlags.None);
        }

        public Task Flush()
        {
            var waitValue = ++value;
            SilkMarshal.ThrowHResult(commandQueue.Signal(fence, waitValue));

            var ev = new ManualResetEvent(false);
            unsafe
            {
                SilkMarshal.ThrowHResult(fence.SetEventOnCompletion(waitValue, (void*)ev.Handle));
            }
            return ev.WaitOneAsync();
        }

        public void Dispose()
        {
            fence.Dispose();
        }
    }
}
