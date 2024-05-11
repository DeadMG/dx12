using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using System.Runtime.InteropServices;

namespace Renderer
{
    public static class IUnknownExtensions
    {
        public static bool TryQueryInterface<T>(this ComPtr<ID3D12Device> ptr, out ComPtr<T> result)
            where T : unmanaged, IComVtbl<T>
        {
            result = new ComPtr<T>();
            unsafe
            {
                var hResult = ptr.Handle->QueryInterface(SilkMarshal.GuidPtrOf<T>(), (void**)result.GetAddressOf());
                if (Marshal.GetExceptionForHR(hResult) != null) return false;
            }

            return true;
        }
    }
}
