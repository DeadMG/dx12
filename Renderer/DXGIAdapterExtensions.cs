using Silk.NET.Core.Native;
using Silk.NET.DXGI;

namespace Renderer
{
    public static class DXGIAdapterExtensions
    {
        public static AdapterDesc3 GetDesc(this ComPtr<IDXGIAdapter4> adapter)
        {
            var desc = new AdapterDesc3();
            SilkMarshal.ThrowHResult(adapter.GetDesc3(ref desc));
            return desc;
        }
    }
}
