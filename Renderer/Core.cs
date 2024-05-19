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
            swapChain = commandQueue.CreateSwapChain(hWnd, 3, width, height);
            resourceCache = new ResourceCache(device);

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
            resourceCache.Load(blueprints);
            return resourceCache.Flush().AsTask();
        }

        public async Task Render(World world)
        {
            var commandList = commandQueue.CreateCommandList();

            var modelMatrix = Matrix4x4.CreateFromQuaternion(world.Units[0].Orientation);
            var viewMatrix = Matrix4x4.CreateLookAtLeftHanded(world.CameraPosition, world.CameraPosition + Vector3.Transform(new Vector3(0, 0, 1), world.CameraOrientation), Vector3.Transform(new Vector3(0, 1, 0), world.CameraOrientation));
            var projMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(90f.ToRadians(), (float)width / height, 0.1f, 100.0f);

            var blueprint = world.Units[0].Blueprint;
            var meshData = resourceCache.For(blueprint);

            var rtv = swapChain.PrepareBackBuffer(commandList, new RawColor4 { R = 0, G = 0, B = 0, A = 1.0f });
            commandList.ClearDepthBuffer(depthBuffer, 1f);
            commandList.SetPipeline(pipeline);
            commandList.List.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            commandList.List.SetVertexBuffer(0, new SharpDX.Direct3D12.VertexBufferView 
            {
                SizeInBytes = blueprint.Mesh.Vertices.Length * Marshal.SizeOf<Vertex>(),
                BufferLocation = meshData.VertexBuffer.GPUHandle,
                StrideInBytes = Marshal.SizeOf<Vertex>()
            });
            commandList.List.SetIndexBuffer(new SharpDX.Direct3D12.IndexBufferView
            {
                BufferLocation = meshData.IndexBuffer.GPUHandle,
                SizeInBytes = Marshal.SizeOf<short>() * blueprint.Mesh.Indices.Length,
                Format = SharpDX.DXGI.Format.R16_UInt
            });
            commandList.List.SetViewport(new RawViewportF { Width = width, Height = height, MaxDepth = 1.0f, MinDepth = 0f });
            commandList.List.SetScissorRectangles(new RawRectangle { Left = 0, Top = 0, Bottom = int.MaxValue, Right = int.MaxValue });
            commandList.List.SetRenderTargets(new[] { rtv.Handle }, depthBuffer.Handle);
            commandList.SetGraphicsRoot32BitConstants(modelMatrix * viewMatrix * projMatrix);
            commandList.List.DrawIndexedInstanced(blueprint.Mesh.Indices.Length, 1, 0, 0, 0);

            swapChain.Present(commandList);

            await commandQueue.Flush().AsTask();
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
