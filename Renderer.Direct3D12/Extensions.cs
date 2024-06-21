using Data.Space;
using Simulation;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

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

        public static Vortice.Mathematics.Matrix3x4 AsAffine(this Matrix4x4 matrix)
        {
            var worldMatrix = Matrix4x4.Transpose(matrix); // HLSL uses the opposite convention
            return new Vortice.Mathematics.Matrix3x4(
                worldMatrix.M11, worldMatrix.M12, worldMatrix.M13, worldMatrix.M14,
                worldMatrix.M21, worldMatrix.M22, worldMatrix.M23, worldMatrix.M24,
                worldMatrix.M31, worldMatrix.M32, worldMatrix.M33, worldMatrix.M34);
        }

        public static byte[] GetBytes<T>(this T str)
            where T : unmanaged
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }

        public static T GetRandom<T>(this RandomNumberGenerator random)
            where T : unmanaged
        {
            var buffer = new byte[Marshal.SizeOf<T>()];
            random.GetBytes(buffer);

            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            try
            {
                Marshal.Copy(buffer, 0, ptr, Marshal.SizeOf<T>());
                return Marshal.PtrToStructure<T>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
