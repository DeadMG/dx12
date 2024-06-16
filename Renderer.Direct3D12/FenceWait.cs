using Util;

namespace Renderer.Direct3D12
{
    internal class FenceWait
    {
        private readonly Vortice.Direct3D12.ID3D12Fence fence;
        private readonly ulong waitValue;

        public FenceWait(Vortice.Direct3D12.ID3D12Fence fence, ulong waitValue)
        {
            this.fence = fence;
            this.waitValue = waitValue;
        }

        public async Task AsTask()
        {
            if (fence.CompletedValue >= waitValue) return;
            Thread.SpinWait(5);
            if (fence.CompletedValue >= waitValue) return;

            using (var ev = new ManualResetEvent(false))
            {
                unsafe
                {
                    fence.SetEventOnCompletion(waitValue, ev.SafeWaitHandle.DangerousGetHandle());
                }
                await ev.WaitOneAsync();
            }
        }

        public FenceWait And(FenceWait other)
        {
            return new FenceWait(fence, Math.Max(waitValue, other.waitValue));
        }
    }
}
