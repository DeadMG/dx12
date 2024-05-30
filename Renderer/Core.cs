using Wrapper.DXGI;
using Wrapper.Direct3D;
using Simulation;
using SharpDX.Mathematics.Interop;
using Data;
using Wrapper;

namespace Renderer
{
    public class Core : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();
        private readonly ResourceCache resourceCache;
        private readonly Wrapper.DXGI.Factory factory;
        private readonly Adapter adapter;
        private readonly Device device;
        private readonly DirectCommandQueue commandQueue;
        private readonly SwapChain swapChain;
        private readonly Pipeline pipeline;
        private readonly DepthBuffer depthBuffer;
        private readonly CopyCommandQueue copyQueue;

        private ScreenSize screenSize;

        public Core(IntPtr hWnd, ScreenSize screenSize)
        {
            this.screenSize = screenSize;

            this.factory = tracker.Track(new Wrapper.DXGI.Factory());

            adapter = tracker.Track(factory.SelectAdapter(adapters => adapters
                .Where(a => !a.IsSoftware)
                .MaxBy(a => a.DedicatedVideoMemory)));

            device = tracker.Track(adapter.CreateDevice());
            commandQueue = tracker.Track(device.CreateDirectCommandQueue());
            copyQueue = tracker.Track(device.CreateCopyCommandQueue());
            swapChain = tracker.Track(commandQueue.CreateSwapChain(hWnd, 3, screenSize));
            resourceCache = tracker.Track(new ResourceCache(device, copyQueue.CreateCommandList()));

            factory.IgnoreAltEnter(hWnd);

            pipeline = tracker.Track(device.CreatePipeline(new PipelineDescriptor
            {
                PixelShader = new Shader { Filename = "pixel.hlsl", EntryPoint = "main" },
                VertexShader = new Shader { Filename = "vertex.hlsl", EntryPoint = "main" },
                RenderTargetFormats = [swapChain.RenderTargetFormat]
            }));

            depthBuffer = tracker.Track(device.CreateDepthBuffer(screenSize));
        }

        public Task Load(Blueprint[] blueprints)
        {
            return resourceCache.Load(blueprints).AsTask();
        }

        public async Task Render(Game simulation, Player player)
        {
            var commandList = commandQueue.CreateCommandList();
            var world = player.ViewingWorld(simulation);
            var vpMatrix = player.CameraFor(world).ViewProjectionMatrix(screenSize);

            var rtv = swapChain.PrepareBackBuffer(commandList, new RawColor4 { R = 0, G = 0, B = 0, A = 1.0f });
            commandList.ClearDepthBuffer(depthBuffer, 1f);
            commandList.Execute();

            using (var tracker = new DisposeTracker())
            {
                var param = new RendererParameters
                {
                    CommandList = commandList,
                    Device = device,
                    ResourceCache = resourceCache,
                    Tracker = tracker,
                    VPMatrix = vpMatrix,
                    Pipeline = pipeline,
                    ScreenSize = screenSize,
                    DepthBuffer = depthBuffer,
                    RenderTargetView = rtv,
                    Player = player,
                    World = world,
                };

                new WorldRenderer().Render(param);

                var draw = swapChain.BeginDirect2D();

                new OrderRenderer().Render(param, draw);
                new UIRenderer().Render(param, draw);

                commandList.Execute();

                swapChain.Present();

                await commandQueue.Flush().AsTask();
            }
        }

        public void Resize(ScreenSize size)
        {
            this.screenSize = size;

            swapChain.Resize(size);
            depthBuffer.Resize(size);
        }

        public void Dispose()
        {
            tracker.Dispose();
        }
    }
}
