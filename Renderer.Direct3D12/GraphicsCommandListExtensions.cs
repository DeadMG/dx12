using System.Runtime.InteropServices;

namespace Renderer.Direct3D12
{
    public static class GraphicsCommandListExtensions
    {
        public static void SetGraphicsRoot32BitConstants<T>(this SharpDX.Direct3D12.GraphicsCommandList list, int rootParameterIndex, T value)
            where T : unmanaged
        {
            unsafe
            {
                list.SetGraphicsRoot32BitConstants(rootParameterIndex, Marshal.SizeOf<T>() / 4, new IntPtr(&value), 0);
            }
        }
    }
}
