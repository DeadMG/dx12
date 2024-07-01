using Data.Space;
using Platform.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    public class Direct3D12Renderer : IRenderer
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();

        private readonly Vortice.Direct3D12.ID3D12Device5 device;
        private readonly Vortice.Direct3D11.ID3D11Device device11;
        private readonly Vortice.Direct3D11.ID3D11DeviceContext immediateContext;
        private readonly Vortice.Direct3D11on12.ID3D11On12Device on12;
        private readonly CommandListPool directCommandQueue;
        private readonly Vortice.DXGI.IDXGISwapChain3 swapChain;
        private readonly DescriptorHeapAccumulator heapAccumulator;
        private readonly Vortice.Direct2D1.ID2D1DeviceContext deviceContext;
        private readonly Vortice.Direct2D1.ID2D1Factory1 factory1;
        private readonly MeshResourceCache bpResourceCache;
        private readonly RaytracingVolumeRenderer raytraceVolumeRenderer;
        private readonly Direct2DDraw draw;

        private BackBuffers backBuffers;

        public Direct3D12Renderer(IntPtr hWnd, ScreenSize size)
        {
            LoadLibraryW("C:\\Program Files\\Microsoft PIX\\2405.15.002-OneBranch_release\\WinPixGpuCapturer.dll");
            LoadLibraryW("C:\\Program Files\\Microsoft PIX\\2405.15.002-OneBranch_release\\WinPixTimingCapturer.dll");

            using (var debug = Vortice.Direct3D12.D3D12.D3D12GetDebugInterface<Vortice.Direct3D12.Debug.ID3D12Debug>())
            {
                debug?.EnableDebugLayer();
            }
            
            using (var queue = Vortice.DXGI.DXGI.DXGIGetDebugInterface1<Vortice.DXGI.Debug.IDXGIInfoQueue>())
            {
                queue?.SetBreakOnSeverity(Vortice.DXGI.DXGI.DebugAll, Vortice.DXGI.Debug.InfoQueueMessageSeverity.Corruption, true);
                queue?.SetBreakOnSeverity(Vortice.DXGI.DXGI.DebugAll, Vortice.DXGI.Debug.InfoQueueMessageSeverity.Error, true);
                queue?.SetBreakOnSeverity(Vortice.DXGI.DXGI.DebugAll, Vortice.DXGI.Debug.InfoQueueMessageSeverity.Warning, true);
            }

            using (var factory = Vortice.DXGI.DXGI.CreateDXGIFactory2<Vortice.DXGI.IDXGIFactory5>(Debug))
            {
                factory.MakeWindowAssociation(hWnd, Vortice.DXGI.WindowAssociationFlags.IgnoreAltEnter);

                using (var adapters = factory.GetAdapters().DisposeAll())
                {
                    var adapter = adapters.Value
                        .Where(a => !a.Description1.Flags.HasFlag(Vortice.DXGI.AdapterFlags.Software))
                        .MaxBy(a => a.Description1.DedicatedVideoMemory);
                    
                    device = disposeTracker.Track(Vortice.Direct3D12.D3D12.D3D12CreateDevice<Vortice.Direct3D12.ID3D12Device5>(adapter, Vortice.Direct3D.FeatureLevel.Level_12_0).Name("Main device"));
                }

                using (var infoQueue = device.QueryInterfaceOrNull<Vortice.Direct3D12.Debug.ID3D12InfoQueue>())
                {
                    infoQueue?.SetBreakOnSeverity(Vortice.Direct3D12.Debug.MessageSeverity.Corruption, true);
                    infoQueue?.SetBreakOnSeverity(Vortice.Direct3D12.Debug.MessageSeverity.Error, true);
                    infoQueue?.SetBreakOnSeverity(Vortice.Direct3D12.Debug.MessageSeverity.Warning, true);
                
                    infoQueue?.PushStorageFilter(new Vortice.Direct3D12.Debug.InfoQueueFilter
                    {
                        AllowList = new Vortice.Direct3D12.Debug.InfoQueueFilterDescription
                        {
                        },
                        DenyList = new Vortice.Direct3D12.Debug.InfoQueueFilterDescription
                        {
                            Ids = new [] { Vortice.Direct3D12.Debug.MessageId.ClearRenderTargetViewMismatchingClearValue },
                            Severities = new [] { Vortice.Direct3D12.Debug.MessageSeverity.Info }
                        }
                    });
                }

                directCommandQueue = disposeTracker.Track(new CommandListPool(device, device.CreateCommandQueue(new Vortice.Direct3D12.CommandQueueDescription
                {
                    Flags = Vortice.Direct3D12.CommandQueueFlags.None,
                    NodeMask = 0,
                    Priority = (int)Vortice.Direct3D12.CommandQueuePriority.Normal,
                    Type = Vortice.Direct3D12.CommandListType.Direct
                }).Name("Main direct queue")));

                var description = new Vortice.DXGI.SwapChainDescription1
                {
                    Width = Math.Max(size.Width, 1),
                    Height = Math.Max(size.Height, 1),
                    BufferCount = 3,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                    Stereo = false,
                    SampleDescription = new Vortice.DXGI.SampleDescription { Count = 1, Quality = 0 },
                    BufferUsage = Vortice.DXGI.Usage.RenderTargetOutput,
                    Scaling = Vortice.DXGI.Scaling.Stretch,
                    SwapEffect = Vortice.DXGI.SwapEffect.FlipDiscard,
                    AlphaMode = Vortice.DXGI.AlphaMode.Unspecified,
                    Flags = 0,
                };

                using (var temp = factory.CreateSwapChainForHwnd(directCommandQueue.Queue, hWnd, description).Name("Main swap chain"))
                {
                    swapChain = disposeTracker.Track(temp.QueryInterface<Vortice.DXGI.IDXGISwapChain3>());
                }
            }

            heapAccumulator = disposeTracker.Track(new DescriptorHeapAccumulator(device));

            Vortice.Direct3D11on12.Apis.D3D11On12CreateDevice(device, Vortice.Direct3D11.DeviceCreationFlags.BgraSupport | Vortice.Direct3D11.DeviceCreationFlags.Debug, [Vortice.Direct3D.FeatureLevel.Level_12_0], [directCommandQueue.Queue], 0,
                out device11,
                out immediateContext,
                out _);
            device11.DebugName = "D3D11 11on12 device";

            disposeTracker.Track(device11);
            disposeTracker.Track(immediateContext.Name("Main d3d11 immediate context"));

            on12 = disposeTracker.Track(device11.QueryInterface<Vortice.Direct3D11on12.ID3D11On12Device>());
            factory1 = disposeTracker.Track(Vortice.Direct2D1.D2D1.D2D1CreateFactory<Vortice.Direct2D1.ID2D1Factory1>(Vortice.Direct2D1.FactoryType.MultiThreaded, Vortice.Direct2D1.DebugLevel.Warning));

            using (var dxgiDevice = device11.QueryInterface<Vortice.DXGI.IDXGIDevice>())
            using (var device2d = factory1.CreateDevice(dxgiDevice))
            {
                deviceContext = disposeTracker.Track(device2d.CreateDeviceContext(Vortice.Direct2D1.DeviceContextOptions.EnableMultithreadedOptimizations));
            }

            backBuffers = new BackBuffers(device, swapChain, on12, deviceContext, immediateContext);

            bpResourceCache = disposeTracker.Track(new MeshResourceCache(device));           
            raytraceVolumeRenderer = disposeTracker.Track(new RaytracingVolumeRenderer(bpResourceCache, device, directCommandQueue, size, swapChain.Description1.Format));

            draw = new Direct2DDraw(factory1, deviceContext, size);
        }

        public void Resize(ScreenSize size)
        {
            var desc = swapChain.Description1;

            if (desc.Width == size.Width && desc.Height == size.Height) return; // Nothing needed

            backBuffers.Dispose();
            swapChain.ResizeBuffers(desc.BufferCount, size.Width, size.Height, desc.Format, desc.Flags);

            backBuffers = new BackBuffers(device, swapChain, on12, deviceContext, immediateContext);

            draw.Resize(size);
        }

        public async Task Render(VolumeRenderTask? volumeRender, Action<IDraw> uiRenderer)
        {
            heapAccumulator.Reset();
            var currentBuffer = backBuffers.backBuffers[swapChain.CurrentBackBufferIndex];

            var poolEntry = directCommandQueue.GetCommandList();
            poolEntry.List.ResourceBarrierTransition(currentBuffer, Vortice.Direct3D12.ResourceStates.Present, Vortice.Direct3D12.ResourceStates.RenderTarget);
            poolEntry.List.ClearRenderTargetView(heapAccumulator.AddRenderTargetView(currentBuffer), new Vortice.Mathematics.Color4(0, 0, 0, 1.0f));
            poolEntry.Execute();

            if (volumeRender != null)
            {
                raytraceVolumeRenderer.Render(new RendererParameters
                {
                    RenderTarget = currentBuffer,
                }, volumeRender.Volume, volumeRender.Camera);
            }

            on12.AcquireWrappedResources(new[] { backBuffers.wrappedResources[swapChain.CurrentBackBufferIndex] }, 1);
            
            deviceContext.Target = backBuffers.d2dRenderTargets[swapChain.CurrentBackBufferIndex];
            deviceContext.BeginDraw();
            deviceContext.Transform = Matrix3x2.Identity;
            
            uiRenderer(draw);
            
            deviceContext.EndDraw();
            
            on12.ReleaseWrappedResources(new[] { backBuffers.wrappedResources[swapChain.CurrentBackBufferIndex] }, 1);
            immediateContext.Flush();

            swapChain.Present(1, Vortice.DXGI.PresentFlags.None);
            deviceContext.Target = null;
            
            await directCommandQueue.Flush().AsTask();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string fileName);

        public void Dispose()
        {
            backBuffers.Dispose();
            disposeTracker.Dispose();
        }

        private const Vortice.Direct3D12.Feature Options5 = (Vortice.Direct3D12.Feature)27;

        // Surprising the compiler doesn't consider ref obj as a possible write
        private struct Direct3D12Options5
        {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value 0
            public uint SRVOnlyTiledResourceTier3;
            public D3D12_RENDER_PASS_TIER RenderPassesTier;
            public Direct3D12RaytracingTier RaytracingTier;
#pragma warning restore CS0649
        }

        private enum D3D12_RENDER_PASS_TIER : uint
        {
            Tier0 = 0,
            Tier1 = 1,
            Tier2 = 2
        }

        private enum Direct3D12RaytracingTier : uint
        {
            NotSupported = 0,
            Tier1_0 = 10,
            Tier1_1 = 11
        }

#if DEBUG
        private bool Debug => true;
#else
        private bool Debug => false;
#endif
    }
}
