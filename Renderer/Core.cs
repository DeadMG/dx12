using Silk.NET.DXGI;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.Core.Contexts;
using System.Runtime.InteropServices;
using Silk.NET.Core;

namespace Renderer
{
    class Core : IDisposable
    {
        private readonly ComPtr<IDXGIFactory5> dxgiFactory;
        private readonly ComPtr<IDXGIAdapter4> dxgiAdapter;
        private readonly ComPtr<ID3D12Device> device;
        private readonly ComPtr<ID3D12CommandQueue> commandQueue;
        private readonly ComPtr<IDXGISwapChain4> swapChain;
        private readonly ComPtr<ID3D12DescriptorHeap> descriptorHeap;
        private readonly List<ComPtr<ID3D12Resource>> backBuffers = new List<ComPtr<ID3D12Resource>>(swapChainBuffers);
        private readonly ComPtr<ID3D12CommandAllocator> commandAllocator;
        private readonly ComPtr<ID3D12GraphicsCommandList> commandList;
        private readonly Fence fence;

        private int currentBackbuffer = 0;

        public Core(IntPtr hWnd)
        {
            dxgiFactory = DXGI.GetApi(new NativeWindowSource(hWnd)).CreateDXGIFactory2<IDXGIFactory5>(dxgiFactoryFlags());
            dxgiAdapter = dxgiFactory.EnumAdapters<IDXGIAdapter4>()
                .Where(a => !a.GetDesc().Flags.HasFlag(AdapterFlag3.Software))
                .MaxBy(a => a.GetDesc().DedicatedVideoMemory);
            device = D3D12.GetApi().CreateDevice<IDXGIAdapter4, ID3D12Device>(dxgiAdapter, D3DFeatureLevel.Level120);

            if (device.TryQueryInterface<ID3D12InfoQueue>(out var infoQueue))
            {
                infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
                infoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
                infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, true);
            }

            commandQueue = device.CreateCommandQueue<ID3D12CommandQueue>(new CommandQueueDesc
            {
                Priority = 1,
                Flags = CommandQueueFlags.None,
                NodeMask = 0,
                Type = CommandListType.Direct
            });

            swapChain = dxgiFactory.CreateSwapChain(commandQueue.QueryInterface<IUnknown>(), hWnd, new SwapChainDesc1
            {
                Width = 0,
                Height = 0,
                Format = Format.FormatR8G8B8A8Unorm,
                Stereo = new Bool32(false),
                SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.Discard,
                AlphaMode = AlphaMode.Unspecified,
                Flags = dxgiFactory.CheckAllowTearing() ? DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING : 0,
            });

            SilkMarshal.ThrowHResult(dxgiFactory.MakeWindowAssociation(hWnd, DXGI_MWA_NO_ALT_ENTER));

            descriptorHeap = device.CreateDescriptorHeap<ID3D12DescriptorHeap>(new DescriptorHeapDesc
            {
                Type = DescriptorHeapType.Rtv,
                NumDescriptors = swapChainBuffers
            });

            var size = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
            var handle = descriptorHeap.GetCPUDescriptorHandleForHeapStart();

            for (uint i = 0; i < swapChainBuffers; ++i)
            {
                var backBuffer = swapChain.GetBuffer<ID3D12Resource>(i);
                unsafe
                {
                    device.CreateRenderTargetView(backBuffer, null, handle);
                }
                backBuffers.Add(backBuffer);
                handle.Ptr += size;
            }

            commandAllocator = device.CreateCommandAllocator<ID3D12CommandAllocator>(CommandListType.Direct);
            commandList = device.CreateCommandList<ID3D12CommandAllocator, ID3D12PipelineState, ID3D12GraphicsCommandList>(0, CommandListType.Direct, commandAllocator, null);
            fence = new Fence(device, commandQueue);
        }

        public unsafe async Task Render()
        {
            var backBuffer = backBuffers[currentBackbuffer];
            SilkMarshal.ThrowHResult(commandAllocator.Reset());
            SilkMarshal.ThrowHResult(commandList.Reset<ID3D12CommandAllocator, ID3D12PipelineState>(commandAllocator, null));
            var barrier = new ResourceBarrier(ResourceBarrierType.Transition, ResourceBarrierFlags.None, null, new ResourceTransitionBarrier(backBuffer.Handle, null, ResourceStates.Present, ResourceStates.RenderTarget), null);
            commandList.ResourceBarrier(1, ref barrier);
            commandList.ClearRenderTargetView(descriptorHeap.GetCPUDescriptorHandleForHeapStart(), new float[] { 0.4f, 0.6f, 0.9f, 1.0f }, 0, null);
        }

        const int swapChainBuffers = 3;

        public void Dispose()
        {
            foreach (var buffer in backBuffers)
            {
                buffer.Dispose();
            }
            descriptorHeap.Dispose();
            swapChain.Dispose();
            commandQueue.Dispose();
            dxgiAdapter.Dispose();
            device.Dispose();
            dxgiFactory.Dispose();
            commandAllocator.Dispose();
            commandList.Dispose();
            fence.Dispose();
        }

        const uint DXGI_MWA_NO_ALT_ENTER = (1 << 1);
        const uint DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING = 2048;
        const int DXGI_USAGE_RENDER_TARGET_OUTPUT = 1 << (1 + 4);

        private uint dxgiFactoryFlags()
        {
#if DEBUG
            return 1;
#else
            return 0;
#endif
        }

        private class NativeWindowSource : INativeWindowSource
        {
            private readonly IntPtr hWnd;

            public NativeWindowSource(IntPtr hWnd)
            {
                this.hWnd = hWnd;
            }
    
            public INativeWindow? Native => new NativeWindow(hWnd);

            private class NativeWindow : INativeWindow
            {
                private readonly IntPtr hWnd;

                public NativeWindow(IntPtr hWnd)
                {
                    this.hWnd = hWnd;
                }

                public NativeWindowFlags Kind => NativeWindowFlags.Win32;
                public (nint Hwnd, nint HDC, nint HInstance)? Win32 
                {
                    get
                    {
                        return (hWnd, IntPtr.Zero, Marshal.GetHINSTANCE(GetType().Module));
                    }
                }

                public (nint Display, nuint Window)? X11 => throw new NotImplementedException();
                public nint? Cocoa => throw new NotImplementedException();
                public (nint Display, nint Surface)? Wayland => throw new NotImplementedException();
                public nint? WinRT => throw new NotImplementedException();
                public (nint Window, uint Framebuffer, uint Colorbuffer, uint ResolveFramebuffer)? UIKit => throw new NotImplementedException();
                public (nint Display, nint Window)? Vivante => throw new NotImplementedException();
                public (nint Window, nint Surface)? Android => throw new NotImplementedException();
                public nint? Glfw => throw new NotImplementedException();
                public nint? Sdl => throw new NotImplementedException();
                public nint? DXHandle => throw new NotImplementedException();
                public (nint? Display, nint? Surface)? EGL => throw new NotImplementedException();
            }
        }
    }
}
