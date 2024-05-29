namespace Wrapper.DXGI
{
    public class Factory : IDisposable
    {
        private readonly SharpDX.DXGI.Factory5 factory;

        public Factory()
        {
            factory = new SharpDX.DXGI.Factory2(Debug).QueryInterface<SharpDX.DXGI.Factory5>();
            using (var queue = SharpDX.DXGI.InfoQueue.TryCreate())
            {
                queue?.SetBreakOnSeverity(SharpDX.DXGI.DebugId.All, SharpDX.DXGI.InformationQueueMessageSeverity.Corruption, true);
                queue?.SetBreakOnSeverity(SharpDX.DXGI.DebugId.All, SharpDX.DXGI.InformationQueueMessageSeverity.Error, true);
                queue?.SetBreakOnSeverity(SharpDX.DXGI.DebugId.All, SharpDX.DXGI.InformationQueueMessageSeverity.Warning, true);
            }
        }

        public void Dispose()
        {
            factory.Dispose();
        }

        public Adapter SelectAdapter(Func<IEnumerable<Adapter>, Adapter> selector)
        {
            var adapters = factory.Adapters1.Select(x => new Adapter(factory, x)).ToArray();

            Adapter returnValue = null;

            try
            {
                return returnValue = selector(adapters);
            }
            finally
            {
                foreach (var adapter in adapters)
                {
                    if (adapter != returnValue)
                    {
                        adapter.Dispose();
                    }
                }
            }
        }

        public void IgnoreAltEnter(IntPtr hWnd)
        {
            factory.MakeWindowAssociation(hWnd, SharpDX.DXGI.WindowAssociationFlags.IgnoreAltEnter);
        }
#if DEBUG
        private bool Debug => true;
#else
        private bool Debug => false;
#endif
    }
}
