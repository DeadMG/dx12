using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    public class BackBuffers : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();

        public readonly List<RenderTargetView> targetViews = new List<RenderTargetView>();
        public readonly List<SharpDX.Direct3D11.Resource> wrappedResources = new List<SharpDX.Direct3D11.Resource>();
        public readonly List<SharpDX.Direct2D1.Bitmap> d2dRenderTargets = new List<SharpDX.Direct2D1.Bitmap>();
        private readonly SharpDX.Direct3D11.Device device11;

        public BackBuffers(SharpDX.Direct3D12.Device device,
            SharpDX.Direct3D12.DescriptorHeap renderTargetHeap,
            SharpDX.DXGI.SwapChain3 swapChain,
            SharpDX.Direct3D11.Device11On12 on12,
            SharpDX.Direct2D1.DeviceContext deviceContext,
            SharpDX.Direct3D11.Device device11)
        {
            this.device11 = device11;

            var size = device.GetDescriptorHandleIncrementSize(SharpDX.Direct3D12.DescriptorHeapType.RenderTargetView);
            var handle = renderTargetHeap.CPUDescriptorHandleForHeapStart;

            for (int i = 0; i < swapChain.Description1.BufferCount; ++i)
            {
                var backBuffer = disposeTracker.Track(swapChain.GetBackBuffer<SharpDX.Direct3D12.Resource>(i));
                backBuffer.Name = $"Back buffer {i}";
                unsafe
                {
                    device.CreateRenderTargetView(backBuffer, null, handle);

                    on12.CreateWrappedResource(backBuffer,
                        new SharpDX.Direct3D11.D3D11ResourceFlags { BindFlags = (int)SharpDX.Direct3D11.BindFlags.RenderTarget },
                        (int)SharpDX.Direct3D12.ResourceStates.RenderTarget,
                        (int)SharpDX.Direct3D12.ResourceStates.Present,
                        Marshal.GenerateGuidForType(typeof(SharpDX.Direct3D11.Resource)),
                        out var resource11);

                    resource11.DebugName = $"11on12 back buffer {i}";
                    wrappedResources.Add(disposeTracker.Track(resource11));

                    using (var surface = resource11.QueryInterface<SharpDX.DXGI.Surface>())
                    {
                        d2dRenderTargets.Add(disposeTracker.Track(new SharpDX.Direct2D1.Bitmap1(deviceContext, surface, new SharpDX.Direct2D1.BitmapProperties1
                        {
                            BitmapOptions = SharpDX.Direct2D1.BitmapOptions.Target | SharpDX.Direct2D1.BitmapOptions.CannotDraw,
                            PixelFormat = new SharpDX.Direct2D1.PixelFormat { AlphaMode = SharpDX.Direct2D1.AlphaMode.Premultiplied, Format = swapChain.Description1.Format }
                        })));
                    }
                }

                targetViews.Add(new RenderTargetView(backBuffer, handle));
                handle.Ptr += size;
            }
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
            device11.ImmediateContext.Flush();
        }

        public SharpDX.Direct3D12.Resource At(int index)
        {
            return targetViews[index].Buffer;
        }

        public readonly record struct RenderTargetView(SharpDX.Direct3D12.Resource Buffer, SharpDX.Direct3D12.CpuDescriptorHandle DescriptorHandle)
        {
        }
    }
}
