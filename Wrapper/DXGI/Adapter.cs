namespace Wrapper.DXGI
{
    public class Adapter : IDisposable
    {
        private readonly SharpDX.DXGI.Factory5 factory;
        private readonly SharpDX.DXGI.Adapter1 adapter;
        private readonly SharpDX.DXGI.AdapterDescription1 description;

        internal Adapter(SharpDX.DXGI.Factory5 factory, SharpDX.DXGI.Adapter1 adapter)
        {
            this.factory = factory;
            this.description = adapter.Description1;
            this.adapter = adapter;
        }

        public bool IsSoftware => description.Flags.HasFlag(SharpDX.DXGI.AdapterFlags.Software);
        public long DedicatedVideoMemory => ((IntPtr)description.DedicatedVideoMemory).ToInt64();

        public void Dispose()
        {
            adapter.Dispose();
        }

        public Direct3D.Device CreateDevice()
        {
            return new Direct3D.Device(factory, new SharpDX.Direct3D12.Device(adapter, SharpDX.Direct3D.FeatureLevel.Level_12_0));
        }
    }
}
