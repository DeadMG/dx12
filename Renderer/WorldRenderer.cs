using Data;
using SharpDX.Mathematics.Interop;
using Simulation;
using System.Runtime.InteropServices;

namespace Renderer
{
    internal class WorldRenderer
    {
        internal void Render(RendererParameters rp, World world)
        {
            rp.CommandList.List.SetViewport(new RawViewportF { Width = rp.ScreenSize.Width, Height = rp.ScreenSize.Height, MaxDepth = 1.0f, MinDepth = 0f });
            rp.CommandList.List.SetScissorRectangles(new RawRectangle { Left = 0, Top = 0, Bottom = int.MaxValue, Right = int.MaxValue });
            rp.CommandList.List.SetRenderTargets(new[] { rp.RenderTargetView.Handle }, rp.DepthBuffer.Handle);
            rp.CommandList.SetPipeline(rp.Pipeline);
            rp.CommandList.List.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            var byBlueprint = world.Units.ToLookup(u => u.Blueprint);
            foreach (var unitGroup in byBlueprint)
            {
                var unitData = unitGroup
                    .Select(x => new InstanceData { WorldMatrix = x.WorldMatrix })
                    .ToArray();

                var meshData = rp.ResourceCache.For(unitGroup.Key);
                var instanceBuffer = rp.Tracker.Track(rp.Device.CreateUploadBuffer(Marshal.SizeOf<InstanceData>() * unitData.Length));

                instanceBuffer.Upload(unitData);

                rp.CommandList.List.SetVertexBuffer(0, new SharpDX.Direct3D12.VertexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<Vertex>() * unitGroup.Key.Mesh.Vertices.Length,
                    BufferLocation = meshData.VertexBuffer.GPUHandle,
                    StrideInBytes = Marshal.SizeOf<Vertex>()
                });
                rp.CommandList.List.SetVertexBuffer(1, new SharpDX.Direct3D12.VertexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<InstanceData>() * unitData.Length,
                    BufferLocation = instanceBuffer.GPUHandle,
                    StrideInBytes = Marshal.SizeOf<InstanceData>()
                });
                rp.CommandList.List.SetIndexBuffer(new SharpDX.Direct3D12.IndexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<short>() * unitGroup.Key.Mesh.Indices.Length,
                    BufferLocation = meshData.IndexBuffer.GPUHandle,
                    Format = SharpDX.DXGI.Format.R16_UInt
                });
                rp.CommandList.SetGraphicsRoot32BitConstants(rp.VPMatrix);
                rp.CommandList.List.DrawIndexedInstanced(unitGroup.Key.Mesh.Indices.Length, unitData.Length, 0, 0, 0);
            }
        }
    }
}
