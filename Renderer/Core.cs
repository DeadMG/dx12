using Wrapper.DXGI;
using Wrapper.Direct3D;
using Simulation;
using SharpDX.Mathematics.Interop;
using Data;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Renderer
{
    public class Core : IDisposable
    {
        private readonly ResourceCache resourceCache;
        private readonly Factory factory = new Factory();
        private readonly Adapter adapter;
        private readonly Device device;
        private readonly DirectCommandQueue commandQueue;
        private readonly SwapChain swapChain;
        private readonly Pipeline pipeline;
        private readonly DepthBuffer depthBuffer;
        private readonly CopyCommandQueue copyQueue;
        private int width;
        private int height;

        public Core(IntPtr hWnd, int width, int height)
        {
            this.width = width;
            this.height = height;

            adapter = factory.SelectAdapter(adapters => adapters
                .Where(a => !a.IsSoftware)
                .MaxBy(a => a.DedicatedVideoMemory));

            device = adapter.CreateDevice();
            commandQueue = device.CreateDirectCommandQueue();
            copyQueue = device.CreateCopyCommandQueue();
            swapChain = commandQueue.CreateSwapChain(hWnd, 3, width, height);
            resourceCache = new ResourceCache(device, copyQueue.CreateCommandList());

            factory.IgnoreAltEnter(hWnd);

            pipeline = device.CreatePipeline(new PipelineDescriptor
            {
                PixelShader = new Shader { Filename = "pixel.hlsl", EntryPoint = "main" },
                VertexShader = new Shader { Filename = "vertex.hlsl", EntryPoint = "main" },
                RenderTargetFormats = [swapChain.RenderTargetFormat]
            });

            depthBuffer = device.CreateDepthBuffer(width, height);
        }

        public Task Load(Blueprint[] blueprints)
        {
            return resourceCache.Load(blueprints).AsTask();
        }

        public async Task Render(Game simulation, Player player)
        {
            var commandList = commandQueue.CreateCommandList();
            var world = player.ViewingWorld(simulation);
            var vpMatrix = player.CameraFor(world).ViewProjectionMatrix(width, height);

            var byBlueprint = world.Units.ToLookup(u => u.Blueprint);

            var rtv = swapChain.PrepareBackBuffer(commandList, new RawColor4 { R = 0, G = 0, B = 0, A = 1.0f });
            commandList.ClearDepthBuffer(depthBuffer, 1f);
            commandList.SetPipeline(pipeline);
            commandList.List.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            commandList.List.SetViewport(new RawViewportF { Width = width, Height = height, MaxDepth = 1.0f, MinDepth = 0f });
            commandList.List.SetScissorRectangles(new RawRectangle { Left = 0, Top = 0, Bottom = int.MaxValue, Right = int.MaxValue });
            commandList.List.SetRenderTargets(new[] { rtv.Handle }, depthBuffer.Handle);

            var instanceBuffers = new List<IDisposable>();
            foreach (var unitGroup in byBlueprint)
            {
                var unitData = unitGroup
                    .Select(x => new InstanceData { ModelMatrix = Matrix4x4.CreateFromQuaternion(x.Orientation) * Matrix4x4.CreateTranslation(x.Position) })
                    .ToArray();

                var meshData = resourceCache.For(unitGroup.Key);
                var instanceBuffer = device.CreateUploadBuffer(Marshal.SizeOf<InstanceData>() * unitData.Length);

                instanceBuffer.Upload(unitData);
                instanceBuffers.Add(instanceBuffer);

                commandList.List.SetVertexBuffer(0, new SharpDX.Direct3D12.VertexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<Vertex>() * unitGroup.Key.Mesh.Vertices.Length,
                    BufferLocation = meshData.VertexBuffer.GPUHandle,
                    StrideInBytes = Marshal.SizeOf<Vertex>()
                });
                commandList.List.SetVertexBuffer(1, new SharpDX.Direct3D12.VertexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<InstanceData>() * unitData.Length,
                    BufferLocation = instanceBuffer.GPUHandle,
                    StrideInBytes = Marshal.SizeOf<InstanceData>()
                });
                commandList.List.SetIndexBuffer(new SharpDX.Direct3D12.IndexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<short>() * unitGroup.Key.Mesh.Indices.Length,
                    BufferLocation = meshData.IndexBuffer.GPUHandle,
                    Format = SharpDX.DXGI.Format.R16_UInt
                });
                commandList.SetGraphicsRoot32BitConstants(vpMatrix);
                commandList.List.DrawIndexedInstanced(unitGroup.Key.Mesh.Indices.Length, unitData.Length, 0, 0, 0);
            }

            swapChain.Present(commandList);

            await commandQueue.Flush().AsTask();

            foreach(var buffer in instanceBuffers)
            {
                buffer.Dispose();
            }
        }

        public void Resize(int width, int height)
        {
            this.width = width;
            this.height = height;

            swapChain.Resize(width, height);
            depthBuffer.Resize(width, height);
        }

        public void Dispose()
        {
            copyQueue.Dispose();
            depthBuffer.Dispose();
            pipeline.Dispose();
            commandQueue.Dispose();
            swapChain.Dispose();
            adapter.Dispose();
            device.Dispose();
            factory.Dispose();
        }
    }
}
