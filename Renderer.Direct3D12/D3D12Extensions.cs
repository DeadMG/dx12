namespace Renderer.Direct3D12
{
    internal static class D3D12Extensions
    {
        public static T Name<T>(this T child, string name)
            where T : Vortice.Direct3D12.ID3D12Object
        {
            child.Name = name;
            return child;
        }
    }
}
