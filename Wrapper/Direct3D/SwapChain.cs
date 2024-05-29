using Data;
using SharpDX.Diagnostics;
using SharpDX.Mathematics.Interop;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Wrapper.Direct2D;

namespace Wrapper.Direct3D
{
    public class SwapChain : IDisposable
    {
        private readonly DisposeTracker lifeTracker = new DisposeTracker();
        private DisposeTracker sizeTracker = new DisposeTracker();

        private readonly SharpDX.Direct3D12.CommandQueue d3d11queue;
        private readonly SharpDX.Direct3D11.Device device11;
        private readonly SharpDX.Direct2D1.DeviceContext deviceContext;
        private readonly SharpDX.Direct3D12.Device device;
        private readonly SharpDX.DXGI.SwapChain3 swapChain;
        private readonly SharpDX.Direct3D12.DescriptorHeap descriptorHeap;
        private readonly SharpDX.Direct3D11.Device11On12 on12;
        private readonly SharpDX.Direct2D1.Device deviced2d;
        private readonly SharpDX.DXGI.Device dxgiDevice;
        private readonly List<RenderTargetView> backBuffers = new List<RenderTargetView>();
        private readonly List<SharpDX.Direct3D11.Resource> wrappedResources = new List<SharpDX.Direct3D11.Resource>();
        private readonly List<SharpDX.Direct2D1.Bitmap> d2dRenderTargets = new List<SharpDX.Direct2D1.Bitmap>();
        private readonly SharpDX.DirectWrite.Factory dwriteFactory;

        public SwapChain(SharpDX.Direct3D12.Device device, SharpDX.Direct3D12.CommandQueue commandQueue, SharpDX.DXGI.SwapChain3 swapChain)
        {
            this.device = device;
            this.swapChain = lifeTracker.Track(swapChain);
            this.d3d11queue = commandQueue;

            descriptorHeap = lifeTracker.Track(device.CreateDescriptorHeap(new SharpDX.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = swapChain.Description1.BufferCount,
                Flags = SharpDX.Direct3D12.DescriptorHeapFlags.None,
                NodeMask = 0,
                Type = SharpDX.Direct3D12.DescriptorHeapType.RenderTargetView
            }));

            dwriteFactory = lifeTracker.Track(new SharpDX.DirectWrite.Factory());

            device11 = lifeTracker.Track(SharpDX.Direct3D11.Device.CreateFromDirect3D12(device, SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport | SharpDX.Direct3D11.DeviceCreationFlags.Debug, null, null, [d3d11queue]));

            on12 = lifeTracker.Track(device11.QueryInterface<SharpDX.Direct3D11.Device11On12>());
            dxgiDevice = lifeTracker.Track(device11.QueryInterface<SharpDX.DXGI.Device>());

            deviced2d = lifeTracker.Track(new SharpDX.Direct2D1.Device(dxgiDevice, new SharpDX.Direct2D1.CreationProperties { DebugLevel = SharpDX.Direct2D1.DebugLevel.Warning }));
            deviceContext = lifeTracker.Track(new SharpDX.Direct2D1.DeviceContext(deviced2d, SharpDX.Direct2D1.DeviceContextOptions.EnableMultithreadedOptimizations));

            CreateBackBuffers();
        }

        public RenderTargetFormat RenderTargetFormat => new RenderTargetFormat { Format = swapChain.Description1.Format };

        private void CreateBackBuffers()
        {
            var size = device.GetDescriptorHandleIncrementSize(SharpDX.Direct3D12.DescriptorHeapType.RenderTargetView);
            var handle = descriptorHeap.CPUDescriptorHandleForHeapStart;

            for (int i = 0; i < swapChain.Description1.BufferCount; ++i)
            {
                var backBuffer = sizeTracker.Track(swapChain.GetBackBuffer<SharpDX.Direct3D12.Resource>(i));
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
                    wrappedResources.Add(sizeTracker.Track(resource11));
                    
                    using (var surface = resource11.QueryInterface<SharpDX.DXGI.Surface>())
                    {
                        d2dRenderTargets.Add(sizeTracker.Track(new SharpDX.Direct2D1.Bitmap1(deviceContext, surface, new SharpDX.Direct2D1.BitmapProperties1
                        {
                            BitmapOptions = SharpDX.Direct2D1.BitmapOptions.Target | SharpDX.Direct2D1.BitmapOptions.CannotDraw,
                            PixelFormat = new SharpDX.Direct2D1.PixelFormat { AlphaMode = SharpDX.Direct2D1.AlphaMode.Premultiplied, Format = swapChain.Description1.Format }
                        })));
                    }
                }

                backBuffers.Add(new RenderTargetView(backBuffer, handle));
                handle.Ptr += size;
            }
        }

        public void Resize(ScreenSize size)
        {
            var width = Math.Max(size.Width, 1);
            var height = Math.Max(size.Height, 1);

            var desc = swapChain.Description1;

            if (desc.Width == width && desc.Height == height) return; // Nothing needed

            sizeTracker.Dispose();

            wrappedResources.Clear();
            backBuffers.Clear();
            d2dRenderTargets.Clear();

            device11.ImmediateContext.Flush();
            sizeTracker = new DisposeTracker();
            swapChain.ResizeBuffers(desc.BufferCount, width, height, desc.Format, desc.Flags);

            CreateBackBuffers();
        }

        public DrawContext BeginDirect2D()
        {
            on12.AcquireWrappedResources(new[] { wrappedResources[swapChain.CurrentBackBufferIndex] }, 1);

            deviceContext.Target = d2dRenderTargets[swapChain.CurrentBackBufferIndex];
            deviceContext.BeginDraw();
            deviceContext.Transform = new RawMatrix3x2(1, 0, 0, 1, 0, 0);

            return new DrawContext(deviceContext);
        }

        public void Present()
        {
            deviceContext.EndDraw();
            
            on12.ReleaseWrappedResources(new[] { wrappedResources[swapChain.CurrentBackBufferIndex] }, 1);
            device11.ImmediateContext.Flush();

            swapChain.Present(1, SharpDX.DXGI.PresentFlags.None);
            deviceContext.Target = null;
        }

        public RenderTargetView PrepareBackBuffer(DirectCommandList list, RawColor4? clearColor) 
        {
            var currentBuffer = backBuffers[swapChain.CurrentBackBufferIndex];
            list.Barrier(currentBuffer, SharpDX.Direct3D12.ResourceStates.Present, SharpDX.Direct3D12.ResourceStates.RenderTarget);
            if (clearColor.HasValue)
            {
                list.Clear(currentBuffer, clearColor.Value);
            }
            return currentBuffer;
        }

        public void Dispose()
        {
            sizeTracker.Dispose();
            device11.ImmediateContext.Flush();
            lifeTracker.Dispose();
        }
    }
}
