using Data.Space;

namespace Renderer.Direct3D12
{
    public static class Extensions
    {
        // Convert from "straight" alpha to "premultiplied" alpha
        public static SharpDX.Mathematics.Interop.RawColor4 AsColour4(this RGBA colour)
        {
            return new SharpDX.Mathematics.Interop.RawColor4(colour.R * colour.A, colour.G * colour.A, colour.B * colour.A, colour.A);
        }

        public static SharpDX.Mathematics.Interop.RawVector2 AsRawVector2(this ScreenPosition pos)
        {
            return new SharpDX.Mathematics.Interop.RawVector2 { X = pos.X, Y = pos.Y };
        }

        public static SharpDX.Mathematics.Interop.RawRectangleF AsRawRectangleF(this ScreenRectangle rect)
        {
            return new SharpDX.Mathematics.Interop.RawRectangleF { Top = rect.Start.Y, Left = rect.Start.X, Right = rect.End.X, Bottom = rect.End.Y };
        }

        public static SharpDX.Direct3D12.Resource CreateStaticBuffer(this SharpDX.Direct3D12.Device device, int size)
        {
            return device.CreateCommittedResource(new SharpDX.Direct3D12.HeapProperties(SharpDX.Direct3D12.HeapType.Default),
                SharpDX.Direct3D12.HeapFlags.None,
                SharpDX.Direct3D12.ResourceDescription.Buffer(new SharpDX.Direct3D12.ResourceAllocationInformation { Alignment = 65536, SizeInBytes = size }),
                SharpDX.Direct3D12.ResourceStates.Common);
        }
    }
}
