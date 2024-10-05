using Data.Space;
using Platform.Contracts;
using SharpGen.Runtime;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    public class Direct3D12Renderer : IRenderer
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();

        private readonly Vortice.Direct3D12.ID3D12Device10 device;
        private readonly FrameTracker frameTracker;
        private readonly Vortice.DXGI.IDXGISwapChain3 swapChain;
        private readonly Vortice.Direct3D12.ID3D12CommandQueue directCommandQueue;
        private readonly Vortice.Direct3D12.ID3D12Fence fence;
        private readonly RaytracingVolumeRenderer raytraceVolumeRenderer;
        private readonly PermanentResources permanentResources;

        private BackBuffers backBuffers;

        public Direct3D12Renderer(IntPtr hWnd, ScreenSize size, Options options)
        {
            if (options.PIX)
            {
                LoadLibraryW("C:\\Program Files\\Microsoft PIX\\2408.09\\WinPixGpuCapturer.dll");
                LoadLibraryW("C:\\Program Files\\Microsoft PIX\\2408.09\\WinPixTimingCapturer.dll");
            }
            
            if (options.DXGIBreak)
            {
                using (var queue = Vortice.DXGI.DXGI.DXGIGetDebugInterface1<Vortice.DXGI.Debug.IDXGIInfoQueue>())
                {
                    queue?.SetBreakOnSeverity(Vortice.DXGI.DXGI.DebugAll, Vortice.DXGI.Debug.InfoQueueMessageSeverity.Corruption, true);
                    queue?.SetBreakOnSeverity(Vortice.DXGI.DXGI.DebugAll, Vortice.DXGI.Debug.InfoQueueMessageSeverity.Error, true);
                    queue?.SetBreakOnSeverity(Vortice.DXGI.DXGI.DebugAll, Vortice.DXGI.Debug.InfoQueueMessageSeverity.Warning, true);
                }
            }

            if (options.D3DDebugLayer)
            {
                using (var debug = Vortice.Direct3D12.D3D12.D3D12GetDebugInterface<Vortice.Direct3D12.Debug.ID3D12Debug>())
                {
                    debug?.EnableDebugLayer();
                }
            }

            using (var factory = Vortice.DXGI.DXGI.CreateDXGIFactory2<Vortice.DXGI.IDXGIFactory5>(Debug))
            {
                factory.MakeWindowAssociation(hWnd, Vortice.DXGI.WindowAssociationFlags.IgnoreAltEnter);

                using (var debug = Vortice.Direct3D12.D3D12.D3D12GetDebugInterface<Vortice.Direct3D12.ID3D12DeviceRemovedExtendedDataSettings>())
                {
                    debug?.SetAutoBreadcrumbsEnablement(Vortice.Direct3D12.DredEnablement.ForcedOn);
                    debug?.SetPageFaultEnablement(Vortice.Direct3D12.DredEnablement.ForcedOn);
                }

                using (var adapters = factory.GetAdapters().DisposeAll())
                {
                    var adapter = adapters.Value
                        .Where(a => !a.Description1.Flags.HasFlag(Vortice.DXGI.AdapterFlags.Software))
                        .MaxBy(a => a.Description1.DedicatedVideoMemory);

                    device = disposeTracker.Track(Vortice.Direct3D12.D3D12.D3D12CreateDevice<Vortice.Direct3D12.ID3D12Device10>(adapter, Vortice.Direct3D.FeatureLevel.Level_12_0).Name("Main device"));
                }

                if (options.D3DBreak)
                {
                    using (var infoQueue = device.QueryInterfaceOrNull<Vortice.Direct3D12.Debug.ID3D12InfoQueue>())
                    {
                        infoQueue?.SetBreakOnSeverity(Vortice.Direct3D12.Debug.MessageSeverity.Corruption, true);
                        infoQueue?.SetBreakOnSeverity(Vortice.Direct3D12.Debug.MessageSeverity.Error, true);
                        infoQueue?.SetBreakOnSeverity(Vortice.Direct3D12.Debug.MessageSeverity.Warning, true);
                    }
                }

                directCommandQueue = disposeTracker.Track(device.CreateCommandQueue(new Vortice.Direct3D12.CommandQueueDescription
                {
                    Flags = Vortice.Direct3D12.CommandQueueFlags.None,
                    NodeMask = 0,
                    Priority = (int)Vortice.Direct3D12.CommandQueuePriority.Normal,
                    Type = Vortice.Direct3D12.CommandListType.Direct
                }).Name("Main direct queue"));

                fence = disposeTracker.Track(device.CreateFence());

                var description = new Vortice.DXGI.SwapChainDescription1
                {
                    Width = Math.Max(size.Width, (ushort)1),
                    Height = Math.Max(size.Height, (ushort)1),
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

                using (var temp = factory.CreateSwapChainForHwnd(directCommandQueue, hWnd, description).Name("Main swap chain"))
                {
                    swapChain = disposeTracker.Track(temp.QueryInterface<Vortice.DXGI.IDXGISwapChain3>());
                }
            }

            if (!device.Options12.EnhancedBarriersSupported) throw new InvalidOperationException("No enhanced barriers");

            var model = device.CheckHighestShaderModel(Vortice.Direct3D12.ShaderModel.HighestShaderModel);

            permanentResources = disposeTracker.Track(new PermanentResources(device));
            frameTracker = disposeTracker.Track(new FrameTracker(permanentResources, directCommandQueue, fence));
            backBuffers = new BackBuffers(swapChain);
      
            raytraceVolumeRenderer = disposeTracker.Track(new RaytracingVolumeRenderer(permanentResources, size, swapChain.Description1.Format));
        }

        public void Resize(ScreenSize size)
        {
            var desc = swapChain.Description1;

            if (desc.Width == size.Width && desc.Height == size.Height) return; // Nothing needed

            backBuffers.Dispose();
            swapChain.ResizeBuffers(desc.BufferCount, size.Width, size.Height, desc.Format, desc.Flags);

            backBuffers = new BackBuffers(swapChain);
        }

        public Task Render(VolumeRenderTask volumeRender, Action<IDraw> uiRenderer)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using (var frameLease = frameTracker.Get())
                    {
                        var currentBuffer = backBuffers.backBuffers[swapChain.CurrentBackBufferIndex];
                        var commandList = permanentResources.CommandList;

                        commandList.Barrier(new Vortice.Direct3D12.BarrierGroup([new Vortice.Direct3D12.TextureBarrier
                        {
                            Resource = currentBuffer,
                            SyncBefore = Vortice.Direct3D12.BarrierSync.None,
                            SyncAfter = Vortice.Direct3D12.BarrierSync.Copy,
                            AccessBefore = Vortice.Direct3D12.BarrierAccess.NoAccess,
                            AccessAfter = Vortice.Direct3D12.BarrierAccess.CopyDestination,
                            LayoutBefore = Vortice.Direct3D12.BarrierLayout.Undefined,
                            LayoutAfter = Vortice.Direct3D12.BarrierLayout.CopyDestination,
                        }]));

                        raytraceVolumeRenderer.Render(frameLease.Resources, currentBuffer, volumeRender.Volume, volumeRender.Camera);

                        commandList.Barrier(new Vortice.Direct3D12.BarrierGroup([new Vortice.Direct3D12.TextureBarrier
                        {
                            Resource = currentBuffer,
                            SyncBefore = Vortice.Direct3D12.BarrierSync.Copy,
                            SyncAfter = Vortice.Direct3D12.BarrierSync.None,
                            AccessBefore = Vortice.Direct3D12.BarrierAccess.CopyDestination,
                            AccessAfter = Vortice.Direct3D12.BarrierAccess.NoAccess,
                            LayoutBefore = Vortice.Direct3D12.BarrierLayout.CopyDestination,
                            LayoutAfter = Vortice.Direct3D12.BarrierLayout.Present,
                        }]));

                        commandList.Close();
                        directCommandQueue.ExecuteCommandList(commandList);

                        swapChain.Present(1, Vortice.DXGI.PresentFlags.None);
                    }

                    await frameTracker.Wait().AsTask();
                } 
                catch(SharpGenException ex)
                {
                    if (ex.ResultCode.Code == -2005270523)
                    {
                        using (var dred = device.QueryInterface<Vortice.Direct3D12.ID3D12DeviceRemovedExtendedData2>())
                        {
                            dred.GetAutoBreadcrumbsOutput(out var breadcrumbs).CheckError();
                            dred.GetAutoBreadcrumbsOutput1(out var breadcrumbs1).CheckError();
                        }
                    }
                    throw;
                }
            });
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
