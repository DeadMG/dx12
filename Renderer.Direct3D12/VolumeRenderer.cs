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

        private readonly Vortice.Direct3D12.ID3D12PipelineState state;
        private readonly Vortice.Direct3D12.ID3D12RootSignature signature;
        private readonly CommandListPool directListPool;

        public VolumeRenderer(Vortice.Direct3D12.ID3D12Device5 device, CommandListPool directListPool, bool supportsRaytracing, Vortice.DXGI.Format renderTargetFormat)
        {
            this.directListPool = directListPool;

            resourceCache = disposeTracker.Track(new ResourceCache(device, supportsRaytracing));

            var flags = Vortice.Direct3D12.RootSignatureFlags.AllowInputAssemblerInputLayout
                | Vortice.Direct3D12.RootSignatureFlags.DenyHullShaderRootAccess
                | Vortice.Direct3D12.RootSignatureFlags.DenyDomainShaderRootAccess
                | Vortice.Direct3D12.RootSignatureFlags.DenyGeometryShaderRootAccess
                | Vortice.Direct3D12.RootSignatureFlags.DenyPixelShaderRootAccess;

            var parameter = new Vortice.Direct3D12.RootParameter1(new Vortice.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<Matrix4x4>() / 4), Vortice.Direct3D12.ShaderVisibility.Vertex);
            var signatureDesc = new Vortice.Direct3D12.RootSignatureDescription1(flags, new[] { parameter });

            signature = disposeTracker.Track(device.CreateRootSignature(signatureDesc));
            
            var desc = new Vortice.Direct3D12.GraphicsPipelineStateDescription
            {
                PrimitiveTopologyType = Vortice.Direct3D12.PrimitiveTopologyType.Triangle,
                DepthStencilFormat = Vortice.DXGI.Format.D32_Float,
                RootSignature = signature,
                PixelShader = Shader.Load("pixel.hlsl", "main", "ps_5_1"),
                VertexShader = Shader.Load("vertex.hlsl", "main", "vs_5_1"),
                RenderTargetFormats = [renderTargetFormat],
                InputLayout = new Vortice.Direct3D12.InputLayoutDescription([
                    new Vortice.Direct3D12.InputElementDescription("POSITION", 0, Vortice.DXGI.Format.R32G32B32_Float, Vortice.Direct3D12.InputElementDescription.AppendAligned, 0, Vortice.Direct3D12.InputClassification.PerVertexData, 0),
                    new Vortice.Direct3D12.InputElementDescription("NORMAL", 0, Vortice.DXGI.Format.R32G32B32_Float, Vortice.Direct3D12.InputElementDescription.AppendAligned, 0, Vortice.Direct3D12.InputClassification.PerVertexData, 0),
                    new Vortice.Direct3D12.InputElementDescription("COLOR", 0, Vortice.DXGI.Format.R32G32B32_Float, Vortice.Direct3D12.InputElementDescription.AppendAligned, 0, Vortice.Direct3D12.InputClassification.PerVertexData, 0),
                    new Vortice.Direct3D12.InputElementDescription("INSTANCE_TRANSFORM", 0, Vortice.DXGI.Format.R32G32B32A32_Float, Vortice.Direct3D12.InputElementDescription.AppendAligned, 1, Vortice.Direct3D12.InputClassification.PerInstanceData, 1),
                    new Vortice.Direct3D12.InputElementDescription("INSTANCE_TRANSFORM", 1, Vortice.DXGI.Format.R32G32B32A32_Float, Vortice.Direct3D12.InputElementDescription.AppendAligned, 1, Vortice.Direct3D12.InputClassification.PerInstanceData, 1),
                    new Vortice.Direct3D12.InputElementDescription("INSTANCE_TRANSFORM", 2, Vortice.DXGI.Format.R32G32B32A32_Float, Vortice.Direct3D12.InputElementDescription.AppendAligned, 1, Vortice.Direct3D12.InputClassification.PerInstanceData, 1),
                    new Vortice.Direct3D12.InputElementDescription("INSTANCE_TRANSFORM", 3, Vortice.DXGI.Format.R32G32B32A32_Float, Vortice.Direct3D12.InputElementDescription.AppendAligned, 1, Vortice.Direct3D12.InputClassification.PerInstanceData, 1),
                ]),
                DepthStencilState = new Vortice.Direct3D12.DepthStencilDescription
                {
                    DepthFunc = Vortice.Direct3D12.ComparisonFunction.Less,
                    DepthEnable = true,
                    DepthWriteMask = Vortice.Direct3D12.DepthWriteMask.All,
                    StencilEnable = false,
                },
                SampleMask = 0xFFFFFFFF,
                StreamOutput = new Vortice.Direct3D12.StreamOutputDescription { Elements = new Vortice.Direct3D12.StreamOutputElement[0], Strides = new uint[0] },
                RasterizerState = new Vortice.Direct3D12.RasterizerDescription
                {
                    FillMode = Vortice.Direct3D12.FillMode.Solid,
                    CullMode = Vortice.Direct3D12.CullMode.None,
                    ForcedSampleCount = 0,
                    DepthClipEnable = true,
                },
                BlendState = new Vortice.Direct3D12.BlendDescription
                {
                    AlphaToCoverageEnable = false,
                    IndependentBlendEnable = false,
                    RenderTarget = new Vortice.Direct3D12.BlendDescription.RenderTarget__FixedBuffer
                    {
                        e0 = new Vortice.Direct3D12.RenderTargetBlendDescription
                        {
                            RenderTargetWriteMask = Vortice.Direct3D12.ColorWriteEnable.All
                        }
                    }
                },
                SampleDescription = new Vortice.DXGI.SampleDescription
                {
                    Count = 1,
                    Quality = 0
                },
            };

            state = disposeTracker.Track(device.CreateGraphicsPipelineState(desc));
        }

        public void Render(RendererParameters rp, Volume volume, Camera camera)
        {
            if (volume == null) return;

            var entry = directListPool.GetCommandList();

            entry.List.RSSetViewport(new Vortice.Mathematics.Viewport { Width = rp.ScreenSize.Width, Height = rp.ScreenSize.Height, MaxDepth = 1.0f, MinDepth = 0f });
            entry.List.RSSetScissorRects(new Vortice.RawRect(0, 0, int.MaxValue, int.MaxValue));
            entry.List.OMSetRenderTargets(new[] { rp.RenderTargetView }, rp.DepthBuffer);
            entry.List.SetPipelineState(state);
            entry.List.SetGraphicsRootSignature(signature);
            entry.List.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);

            var byBlueprint = volume.Units.ToLookup(u => u.Blueprint);
            foreach (var unitGroup in byBlueprint)
            {
                var unitData = unitGroup
                    .Select(x => x.WorldMatrix)
                    .ToArray();

                var meshData = resourceCache.For(unitGroup.Key, entry);
                var instanceBuffer = rp.Tracker.Track(entry.CreateUploadBuffer(unitData));

                entry.List.IASetVertexBuffers(0, new Vortice.Direct3D12.VertexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<ComputedVertex>() * unitGroup.Key.Mesh.Vertices.Length,
                    BufferLocation = meshData.VertexBuffer.GPUVirtualAddress,
                    StrideInBytes = Marshal.SizeOf<ComputedVertex>()
                });
                entry.List.IASetVertexBuffers(1, new Vortice.Direct3D12.VertexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<Matrix4x4>() * unitData.Length,
                    BufferLocation = instanceBuffer.GPUVirtualAddress,
                    StrideInBytes = Marshal.SizeOf<Matrix4x4>()
                });
                entry.List.IASetIndexBuffer(new Vortice.Direct3D12.IndexBufferView
                {
                    SizeInBytes = Marshal.SizeOf<short>() * unitGroup.Key.Mesh.Indices.Length,
                    BufferLocation = meshData.IndexBuffer.GPUVirtualAddress,
                    Format = Vortice.DXGI.Format.R16_UInt
                });
                entry.List.SetGraphicsRoot32BitConstants(0, camera.ViewProjection, 0);
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
