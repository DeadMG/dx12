namespace Renderer
{
    public class Debugging
    {
        public void ReportLiveObjects()
        {
            using (var debug = SharpDX.DXGI.DXGIDebug.TryCreate())
            {
                debug?.ReportLiveObjects(SharpDX.DXGI.DebugId.All, SharpDX.DXGI.DebugRloFlags.Detail);
            }
        }
    }
}
