namespace Renderer.Direct3D12
{
    internal static class D3D11Extensions
    {
        public static T Name<T>(this T child, string name)
            where T : Vortice.Direct3D11.ID3D11DeviceChild
        {
            child.DebugName = name;
            return child;
        }
    }
}
