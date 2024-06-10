using Data.Space;
using Platform.Contracts;
using SharpDX.Mathematics.Interop;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    public class Direct3D12Renderer : IRenderer
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();

        private readonly SharpDX.Direct3D12.Device device;
        private readonly SharpDX.Direct3D11.Device device11;
        private readonly SharpDX.Direct3D11.Device11On12 on12;
        private readonly CommandListPool directCommandQueue;
        private readonly SharpDX.DXGI.SwapChain3 swapChain;
        private readonly SharpDX.Direct3D12.DescriptorHeap renderTargetHeap;
        private readonly SharpDX.Direct2D1.DeviceContext deviceContext;
        private readonly SharpDX.Direct3D12.DescriptorHeap depthStencilHeap;
        private readonly SharpDX.Direct2D1.Factory1 factory1;
        private readonly VolumeRenderer volumeRenderer;
        private readonly Direct2DDraw draw;

        private DepthBuffer depthBuffer;
        private BackBuffers backBuffers;

        public Direct3D12Renderer(IntPtr hWnd, ScreenSize size)
        {
            LoadLibraryW("C:\\Program Files\\Microsoft PIX\\2405.15.002-OneBranch_release\\WinPixGpuCapturer.dll");
            LoadLibraryW("C:\\Program Files\\Microsoft PIX\\2405.15.002-OneBranch_release\\WinPixTimingCapturer.dll");

            using (var debug = SharpDX.Direct3D12.DebugInterface.Get())
            {
                debug?.EnableDebugLayer();
            }

            using (var queue = SharpDX.DXGI.InfoQueue.TryCreate())
            {
                queue?.SetBreakOnSeverity(SharpDX.DXGI.DebugId.All, SharpDX.DXGI.InformationQueueMessageSeverity.Corruption, true);
                queue?.SetBreakOnSeverity(SharpDX.DXGI.DebugId.All, SharpDX.DXGI.InformationQueueMessageSeverity.Error, true);
                queue?.SetBreakOnSeverity(SharpDX.DXGI.DebugId.All, SharpDX.DXGI.InformationQueueMessageSeverity.Warning, true);
            }

            using (var f = new SharpDX.DXGI.Factory2(Debug))
            using (var factory = f.QueryInterface<SharpDX.DXGI.Factory5>())
            {
                factory.MakeWindowAssociation(hWnd, SharpDX.DXGI.WindowAssociationFlags.IgnoreAltEnter);

                using (var adapters = factory.Adapters1.DisposeAll())
                {
                    var adapter = adapters.Value
                        .Where(a => !a.Description1.Flags.HasFlag(SharpDX.DXGI.AdapterFlags.Software))
                        .MaxBy(a => a.Description1.DedicatedVideoMemory);

                    device = disposeTracker.Track(new SharpDX.Direct3D12.Device(adapter, SharpDX.Direct3D.FeatureLevel.Level_12_0));
                }

                using (var infoQueue = device.QueryInterfaceOrNull<SharpDX.Direct3D12.InfoQueue>())
                {
                    infoQueue?.SetBreakOnSeverity(SharpDX.Direct3D12.MessageSeverity.Corruption, true);
                    infoQueue?.SetBreakOnSeverity(SharpDX.Direct3D12.MessageSeverity.Error, true);
                    infoQueue?.SetBreakOnSeverity(SharpDX.Direct3D12.MessageSeverity.Warning, true);
                    infoQueue?.PushStorageFilter(new SharpDX.Direct3D12.InfoQueueFilter
                    {
                        AllowList = new SharpDX.Direct3D12.InfoQueueFilterDescription
                        {
                        },
                        DenyList = new SharpDX.Direct3D12.InfoQueueFilterDescription
                        {
                            Ids = new [] { SharpDX.Direct3D12.MessageId.ClearrendertargetviewMismatchingclearvalue },
                            Severities = new [] { SharpDX.Direct3D12.MessageSeverity.Information }
                        }
                    });
                }

                directCommandQueue = disposeTracker.Track(new CommandListPool(device, device.CreateCommandQueue(new SharpDX.Direct3D12.CommandQueueDescription
                {
                    Flags = SharpDX.Direct3D12.CommandQueueFlags.None,
                    NodeMask = 0,
                    Priority = (int)SharpDX.Direct3D12.CommandQueuePriority.Normal,
                    Type = SharpDX.Direct3D12.CommandListType.Direct
                })));

                var description = new SharpDX.DXGI.SwapChainDescription1
                {
                    Width = Math.Max(size.Width, 1),
                    Height = Math.Max(size.Height, 1),
                    BufferCount = 3,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    Stereo = false,
                    SampleDescription = new SharpDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
                    Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                    Scaling = SharpDX.DXGI.Scaling.Stretch,
                    SwapEffect = SharpDX.DXGI.SwapEffect.FlipDiscard,
                    AlphaMode = SharpDX.DXGI.AlphaMode.Unspecified,
                    Flags = 0,
                };

                using (var temp = new SharpDX.DXGI.SwapChain1(factory, directCommandQueue.Queue, hWnd, ref description))
                {
                    swapChain = disposeTracker.Track(temp.QueryInterface<SharpDX.DXGI.SwapChain3>());
                }
            }

            renderTargetHeap = disposeTracker.Track(device.CreateDescriptorHeap(new SharpDX.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = swapChain.Description1.BufferCount,
                Flags = SharpDX.Direct3D12.DescriptorHeapFlags.None,
                NodeMask = 0,
                Type = SharpDX.Direct3D12.DescriptorHeapType.RenderTargetView
            }));

            device11 = disposeTracker.Track(SharpDX.Direct3D11.Device.CreateFromDirect3D12(device, SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport | SharpDX.Direct3D11.DeviceCreationFlags.Debug, null, null, [directCommandQueue.Queue]));
            on12 = disposeTracker.Track(device11.QueryInterface<SharpDX.Direct3D11.Device11On12>());
            factory1 = disposeTracker.Track(new SharpDX.Direct2D1.Factory1(SharpDX.Direct2D1.FactoryType.MultiThreaded, SharpDX.Direct2D1.DebugLevel.Warning));

            using (var dxgiDevice = device11.QueryInterface<SharpDX.DXGI.Device>())
            using (var device2d = new SharpDX.Direct2D1.Device(factory1, dxgiDevice))
            {
                deviceContext = disposeTracker.Track(new SharpDX.Direct2D1.DeviceContext(device2d, SharpDX.Direct2D1.DeviceContextOptions.EnableMultithreadedOptimizations));
            }

            depthStencilHeap = disposeTracker.Track(device.CreateDescriptorHeap(new SharpDX.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = 10,
                Flags = SharpDX.Direct3D12.DescriptorHeapFlags.None,
                NodeMask = 0,
                Type = SharpDX.Direct3D12.DescriptorHeapType.DepthStencilView
            }));

            backBuffers = new BackBuffers(device, renderTargetHeap, swapChain, on12, deviceContext, device11);
            depthBuffer = new DepthBuffer(device, size, depthStencilHeap.CPUDescriptorHandleForHeapStart);

            volumeRenderer = new VolumeRenderer(device, directCommandQueue, swapChain.Description1.Format);

            draw = new Direct2DDraw(factory1, deviceContext, size);
        }

        public void Resize(ScreenSize size)
        {
            var width = Math.Max(size.Width, 1);
            var height = Math.Max(size.Height, 1);

            var desc = swapChain.Description1;

            if (desc.Width == width && desc.Height == height) return; // Nothing needed

            backBuffers.Dispose();
            depthBuffer.Dispose();
            swapChain.ResizeBuffers(desc.BufferCount, width, height, desc.Format, desc.Flags);

            backBuffers = new BackBuffers(device, renderTargetHeap, swapChain, on12, deviceContext, device11);
            depthBuffer = new DepthBuffer(device, size, depthStencilHeap.CPUDescriptorHandleForHeapStart);

            draw.Resize(size);
        }

        public async Task Render(VolumeRenderTask? volumeRender, Action<IDraw> uiRenderer)
        {
            var currentBuffer = backBuffers.targetViews[swapChain.CurrentBackBufferIndex];

            var poolEntry = directCommandQueue.GetCommandList();
            poolEntry.List.ResourceBarrierTransition(currentBuffer.Buffer, SharpDX.Direct3D12.ResourceStates.Present, SharpDX.Direct3D12.ResourceStates.RenderTarget);
            poolEntry.List.ClearRenderTargetView(currentBuffer.DescriptorHandle, new RawColor4 { R = 0, G = 0, B = 0, A = 1.0f });
            poolEntry.List.ClearDepthStencilView(depthStencilHeap.CPUDescriptorHandleForHeapStart, SharpDX.Direct3D12.ClearFlags.FlagsDepth, 1f, 0);
            poolEntry.Execute();

            using (var tracker = new DisposeTracker())
            {
                if (volumeRender != null)
                {
                    volumeRenderer.Render(new RendererParameters
                    {
                        DepthBuffer = depthStencilHeap.CPUDescriptorHandleForHeapStart,
                        Tracker = tracker,
                        RenderTargetView = currentBuffer.DescriptorHandle,
                        ScreenSize = new ScreenSize(swapChain.Description1.Width, swapChain.Description1.Height),
                    }, volumeRender.Volume, volumeRender.Camera);
                }

                on12.AcquireWrappedResources(new[] { backBuffers.wrappedResources[swapChain.CurrentBackBufferIndex] }, 1);
                
                deviceContext.Target = backBuffers.d2dRenderTargets[swapChain.CurrentBackBufferIndex];
                deviceContext.BeginDraw();
                deviceContext.Transform = new RawMatrix3x2(1, 0, 0, 1, 0, 0);

                uiRenderer(draw);

                deviceContext.EndDraw();
                
                on12.ReleaseWrappedResources(new[] { backBuffers.wrappedResources[swapChain.CurrentBackBufferIndex] }, 1);
                device11.ImmediateContext.Flush();
                
                swapChain.Present(1, SharpDX.DXGI.PresentFlags.None);
                deviceContext.Target = null;
                
                await directCommandQueue.Flush().AsTask();
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string fileName);

        public void Dispose()
        {
            backBuffers.Dispose();
            depthBuffer.Dispose();
            disposeTracker.Dispose();
        }

#if DEBUG
        private bool Debug => true;
#else
        private bool Debug => false;
#endif
    }
}
