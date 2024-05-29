namespace Wrapper.Direct3D
{
    public class Pipeline : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();
        private readonly SharpDX.Direct3D12.PipelineState state;
        private readonly SharpDX.Direct3D12.RootSignature rootSignature;

        public Pipeline(SharpDX.Direct3D12.PipelineState state, SharpDX.Direct3D12.RootSignature rootSignature)
        {
            this.state = tracker.Track(state);
            this.rootSignature = tracker.Track(rootSignature);
        }

        public SharpDX.Direct3D12.RootSignature RootSignature => rootSignature;
        public SharpDX.Direct3D12.PipelineState State => state;

        public void Dispose()
        {
            tracker.Dispose();
        }
    }
}
