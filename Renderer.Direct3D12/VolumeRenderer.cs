using SharpDX.Mathematics.Interop;
using Simulation;
using System.Numerics;
using System.Runtime.InteropServices;
using Util;

namespace Renderer.Direct3D12
{
    public class VolumeRenderer : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly ResourceCache resourceCache;

        private readonly SharpDX.Direct3D12.PipelineState state;
        private readonly SharpDX.Direct3D12.RootSignature signature;
        private readonly CommandListPool directListPool;

        public VolumeRenderer(SharpDX.Direct3D12.Device device, CommandListPool directListPool, SharpDX.DXGI.Format renderTargetFormat)
        {
            this.directListPool = directListPool;

            resourceCache = disposeTracker.Track(new ResourceCache(device));

            var flags = SharpDX.Direct3D12.RootSignatureFlags.AllowInputAssemblerInputLayout
                | SharpDX.Direct3D12.RootSignatureFlags.DenyHullShaderRootAccess
                | SharpDX.Direct3D12.RootSignatureFlags.DenyDomainShaderRootAccess
                | SharpDX.Direct3D12.RootSignatureFlags.DenyGeometryShaderRootAccess
                | SharpDX.Direct3D12.RootSignatureFlags.DenyPixelShaderRootAccess;

            var parameter = new SharpDX.Direct3D12.RootParameter1(SharpDX.Direct3D12.ShaderVisibility.Vertex, new SharpDX.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<Matrix4x4>() / 4));
            var signatureDesc = new SharpDX.Direct3D12.RootSignatureDescription1(flags, new[] { parameter });

            signature = disposeTracker.Track(device.CreateRootSignature(signatureDesc.Serialize()));

            var desc = new SharpDX.Direct3D12.GraphicsPipelineStateDescription
            {
                PrimitiveTopologyType = SharpDX.Direct3D12.PrimitiveTopologyType.Triangle,
                DepthStencilFormat = SharpDX.DXGI.Format.D32_Float,
                RootSignature = signature,
                PixelShader = Shader.Load("pixel.hlsl", "main", "ps_5_1"),
                VertexShader = Shader.Load("vertex.hlsl", "main", "vs_5_1"),
                RenderTargetCount = 1,
                InputLayout = new SharpDX.Direct3D12.InputLayoutDescription([
                    new SharpDX.Direct3D12.InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, SharpDX.Direct3D12.InputElement.AppendAligned, 0, SharpDX.Direct3D12.InputClassification.PerVertexData, 0),
                    new SharpDX.Direct3D12.InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32_Float, SharpDX.Direct3D12.InputElement.AppendAligned, 0, SharpDX.Direct3D12.InputClassification.PerVertexData, 0),
                    new SharpDX.Direct3D12.InputElement("INSTANCE_TRANSFORM", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, SharpDX.Direct3D12.InputElement.AppendAligned, 1, SharpDX.Direct3D12.InputClassification.PerInstanceData, 1),
                    new SharpDX.Direct3D12.InputElement("INSTANCE_TRANSFORM", 1, SharpDX.DXGI.Format.R32G32B32A32_Float, SharpDX.Direct3D12.InputElement.AppendAligned, 1, SharpDX.Direct3D12.InputClassification.PerInstanceData, 1),
                    new SharpDX.Direct3D12.InputElement("INSTANCE_TRANSFORM", 2, SharpDX.DXGI.Format.R32G32B32A32_Float, SharpDX.Direct3D12.InputElement.AppendAligned, 1, SharpDX.Direct3D12.InputClassification.PerInstanceData, 1),
                    new SharpDX.Direct3D12.InputElement("INSTANCE_TRANSFORM", 3, SharpDX.DXGI.Format.R32G32B32A32_Float, SharpDX.Direct3D12.InputElement.AppendAligned, 1, SharpDX.Direct3D12.InputClassification.PerInstanceData, 1),
                ]),
                DepthStencilState = new SharpDX.Direct3D12.DepthStencilStateDescription
                {
                    DepthComparison = SharpDX.Direct3D12.Comparison.Less,
                    IsDepthEnabled = true,
                    DepthWriteMask = SharpDX.Direct3D12.DepthWriteMask.All,
                    IsStencilEnabled = false,
                },
                SampleMask = -1,
                StreamOutput = new SharpDX.Direct3D12.StreamOutputDescription { Elements = new SharpDX.Direct3D12.StreamOutputElement[0], Strides = new int[0] },
                RasterizerState = new SharpDX.Direct3D12.RasterizerStateDescription
                {
                    FillMode = SharpDX.Direct3D12.FillMode.Solid,
                    CullMode = SharpDX.Direct3D12.CullMode.None,
                    ForcedSampleCount = 0,
                    IsDepthClipEnabled = true,
                },
                BlendState = new SharpDX.Direct3D12.BlendStateDescription
                {
                    AlphaToCoverageEnable = false,
                    IndependentBlendEnable = false,
                },
                SampleDescription = new SharpDX.DXGI.SampleDescription
                {
                    Count = 1,
                    Quality = 0
                },
            };

            desc.BlendState.RenderTarget[0].RenderTargetWriteMask = SharpDX.Direct3D12.ColorWriteMaskFlags.All;
            desc.RenderTargetFormats[0] = renderTargetFormat;

            state = disposeTracker.Track(device.CreateGraphicsPipelineState(desc));
        }

        public void Render(RendererParameters rp, Volume volume, Camera camera)
        {
            if (volume == null) return;

            var entry = directListPool.GetCommandList();

            entry.List.SetViewport(new RawViewportF { Width = rp.ScreenSize.Width, Height = rp.ScreenSize.Height, MaxDepth = 1.0f, MinDepth = 0f });
            entry.List.SetScissorRectangles(new RawRectangle { Left = 0, Top = 0, Bottom = int.MaxValue, Right = int.MaxValue });
            entry.List.SetRenderTargets(new[] { rp.RenderTargetView }, rp.DepthBuffer);
            entry.List.PipelineState = state;
            entry.List.SetGraphicsRootSignature(signature);
            entry.List.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

            var byBlueprint = volume.Units.ToLookup(u => u.Blueprint);
            foreach (var unitGroup in byBlueprint)
            {
                var unitData = unitGroup
                    .Select(x => x.WorldMatrix)
                    .ToArray();

                var meshData = resourceCache.For(unitGroup.Key, entry);
                var instanceBuffer = rp.Tracker.Track(entry.CreateUploadBuffer(unitData));

                entry.List.SetVertexBuffer(0, new SharpDX.Direct3D12.VertexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<Vertex>() * unitGroup.Key.Mesh.Vertices.Length,
                    BufferLocation = meshData.VertexBuffer.GPUVirtualAddress,
                    StrideInBytes = Marshal.SizeOf<Vertex>()
                });
                entry.List.SetVertexBuffer(1, new SharpDX.Direct3D12.VertexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<Matrix4x4>() * unitData.Length,
                    BufferLocation = instanceBuffer.GPUVirtualAddress,
                    StrideInBytes = Marshal.SizeOf<Matrix4x4>()
                });
                entry.List.SetIndexBuffer(new SharpDX.Direct3D12.IndexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<short>() * unitGroup.Key.Mesh.Indices.Length,
                    BufferLocation = meshData.IndexBuffer.GPUVirtualAddress,
                    Format = SharpDX.DXGI.Format.R16_UInt
                });
                entry.List.SetGraphicsRoot32BitConstants(0, camera.ViewProjection);
                entry.List.DrawIndexedInstanced(unitGroup.Key.Mesh.Indices.Length, unitData.Length, 0, 0, 0);
            }

            entry.Execute();
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }
    }
}
