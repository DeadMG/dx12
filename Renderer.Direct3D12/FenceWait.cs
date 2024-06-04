using Util;

namespace Renderer.Direct3D12
{
    public class FenceWait
    {
        private readonly SharpDX.Direct3D12.Fence fence;
        private readonly long waitValue;

        public FenceWait(SharpDX.Direct3D12.Fence fence, long waitValue)
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
