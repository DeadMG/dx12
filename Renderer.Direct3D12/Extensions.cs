using Data.Space;

namespace Renderer.Direct3D12
{
    public static class Extensions
    {
        // Convert from "straight" alpha to "premultiplied" alpha
        public static Vortice.Mathematics.Color AsColour4(this RGBA colour)
        {
            return new Vortice.Mathematics.Color(colour.R * colour.A, colour.G * colour.A, colour.B * colour.A, colour.A);
        }

        public static Vortice.Mathematics.Rect AsRawRectangleF(this ScreenRectangle rect)
        {
            return new Vortice.Mathematics.Rect { Top = rect.Start.Y, Left = rect.Start.X, Right = rect.End.X, Bottom = rect.End.Y };
        }

        public static Vortice.DXGI.IDXGIAdapter1[] GetAdapters(this Vortice.DXGI.IDXGIFactory5 factory)
        {
            var result = new List<Vortice.DXGI.IDXGIAdapter1>();
            for (int index = 0; factory.EnumAdapters1(index, out var adapter).Success; index++)
            {
                result.Add(adapter);
            }

            return result.ToArray();
        }

        public static Vortice.Direct3D12.ID3D12Resource CreateStaticBuffer(this Vortice.Direct3D12.ID3D12Device5 device, uint size)
        {
            return device.CreateCommittedResource(new Vortice.Direct3D12.HeapProperties(Vortice.Direct3D12.HeapType.Default),
                Vortice.Direct3D12.HeapFlags.None,
                Vortice.Direct3D12.ResourceDescription.Buffer(new Vortice.Direct3D12.ResourceAllocationInfo { Alignment = 65536, SizeInBytes = size }),
                Vortice.Direct3D12.ResourceStates.Common);
        }
    }
}
