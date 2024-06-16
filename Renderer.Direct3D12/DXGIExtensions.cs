namespace Renderer.Direct3D12
{
    internal static class DXGIExtensions
    {
        public static T Name<T>(this T child, string name)
            where T : Vortice.DXGI.IDXGIObject
        {
            child.DebugName = name;
            return child;
        }        
    }
}
