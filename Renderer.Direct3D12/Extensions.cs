using Data.Space;

namespace Renderer.Direct3D12
{
    internal static class Extensions
    {
        // Convert from "straight" alpha to "premultiplied" alpha
        public static Vortice.Mathematics.Color AsColour4(this RGBA colour)
        {
            return new Vortice.Mathematics.Color(colour.R * colour.A, colour.G * colour.A, colour.B * colour.A, colour.A);
        }

        public static Vortice.Mathematics.Rect AsRawRectangleF(this ScreenRectangle rect)
        {
            return new Vortice.Mathematics.Rect(rect.Start.X, rect.Start.Y, rect.End.X - rect.Start.X, rect.End.Y - rect.Start.Y);
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

        public static Vortice.Direct3D12.CpuDescriptorHandle CPU(this Vortice.Direct3D12.ID3D12DescriptorHeap heap, int amount)
        {
            using (var device = heap.GetDevice<Vortice.Direct3D12.ID3D12Device>())
            {
                return heap.GetCPUDescriptorHandleForHeapStart().Offset(amount, device.GetDescriptorHandleIncrementSize(heap.Description.Type));
            }
        }

        public static Vortice.Direct3D12.GpuDescriptorHandle GPU(this Vortice.Direct3D12.ID3D12DescriptorHeap heap, int amount)
        {
            using (var device = heap.GetDevice<Vortice.Direct3D12.ID3D12Device>())
            {
                return heap.GetGPUDescriptorHandleForHeapStart().Offset(amount, device.GetDescriptorHandleIncrementSize(heap.Description.Type));
            }
        }
    }
}
