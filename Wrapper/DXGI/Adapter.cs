namespace Wrapper.DXGI
{
    public class Adapter : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();

        private readonly SharpDX.DXGI.Factory5 factory;
        private readonly SharpDX.DXGI.Adapter1 adapter;

        internal Adapter(SharpDX.DXGI.Factory5 factory, SharpDX.DXGI.Adapter1 adapter)
        {
            this.factory = factory;
            this.adapter = disposeTracker.Track(adapter);
        }

        public bool IsSoftware => adapter.Description1.Flags.HasFlag(SharpDX.DXGI.AdapterFlags.Software);
        public long DedicatedVideoMemory => ((IntPtr)adapter.Description1.DedicatedVideoMemory).ToInt64();

        public void Dispose()
        {
            disposeTracker.Dispose();
        }

        public Direct3D.Device CreateDevice()
        {
            return new Direct3D.Device(factory, new SharpDX.Direct3D12.Device(adapter, SharpDX.Direct3D.FeatureLevel.Level_12_0));
        }
    }
}
