using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using System.Runtime.InteropServices;

namespace Renderer
{
    public static class DXGIFactoryExtensions
    {
        public static IEnumerable<ComPtr<T>> EnumAdapters<T>(this ComPtr<IDXGIFactory5> factory)
            where T : unmanaged, IComVtbl<IDXGIAdapter1>, IComVtbl<T>
        {
            var adapter = new ComPtr<T>();
            for (uint i = 0; factory.EnumAdapters1<T>(i, ref adapter) != DXGI_ERROR_NOT_FOUND; ++i)
            {
                yield return adapter;
            }
        }

        public static bool CheckAllowTearing(this ComPtr<IDXGIFactory5> factory)
        {
            uint allowTearing = 0;
            unsafe
            {
                SilkMarshal.ThrowHResult(factory.CheckFeatureSupport(Feature.PresentAllowTearing, &allowTearing, (uint)Marshal.SizeOf<uint>()));
            }
            return allowTearing == 1;
        }

        public static ComPtr<IDXGISwapChain4> CreateSwapChain(this ComPtr<IDXGIFactory5> factory, ComPtr<IUnknown> device, IntPtr hWnd, SwapChainDesc1 chainDesc)
        {
            var swapChain = new ComPtr<IDXGISwapChain1>();
            unsafe
            {
                SilkMarshal.ThrowHResult(factory.CreateSwapChainForHwnd(device, hWnd, ref chainDesc, null, null, swapChain.GetAddressOf()));
            }
            return swapChain.QueryInterface<IDXGISwapChain4>();
        }

        private const int DXGI_ERROR_NOT_FOUND = -0x7785FFFE;
    }
}
