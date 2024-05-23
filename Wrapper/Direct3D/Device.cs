using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Wrapper.Direct3D
{
    public class Device : IDisposable
    {
        private readonly SharpDX.DXGI.Factory5 factory;
        private readonly SharpDX.Direct3D12.Device device;
        private readonly DescriptorHeapPool depthStencilPool;
        private readonly ShaderPool shaderPool = new ShaderPool();

        internal Device(SharpDX.DXGI.Factory5 factory, SharpDX.Direct3D12.Device device)
        {
            this.factory = factory;
            this.device = device;

            using (var infoQueue = device.QueryInterfaceOrNull<SharpDX.Direct3D12.InfoQueue>())
            {
                infoQueue?.SetBreakOnSeverity(SharpDX.Direct3D12.MessageSeverity.Corruption, true);
                infoQueue?.SetBreakOnSeverity(SharpDX.Direct3D12.MessageSeverity.Error, true);
                infoQueue?.SetBreakOnSeverity(SharpDX.Direct3D12.MessageSeverity.Warning, true);
            }

            depthStencilPool = new DescriptorHeapPool(device, new SharpDX.Direct3D12.DescriptorHeapDescription
            {
                DescriptorCount = 10,
                Flags = SharpDX.Direct3D12.DescriptorHeapFlags.None,
                NodeMask = 0,
                Type = SharpDX.Direct3D12.DescriptorHeapType.DepthStencilView
            });
        }

        internal SharpDX.Direct3D12.Device Native => device;

        public DirectCommandQueue CreateDirectCommandQueue()
        {
            return new DirectCommandQueue(factory, this, device.CreateCommandQueue(new SharpDX.Direct3D12.CommandQueueDescription
            {
                Flags = SharpDX.Direct3D12.CommandQueueFlags.None,
                NodeMask = 0,
                Priority = (int)SharpDX.Direct3D12.CommandQueuePriority.Normal,
                Type = SharpDX.Direct3D12.CommandListType.Direct
            }));
        }
        
        public CopyCommandQueue CreateCopyCommandQueue()
        {
            return new CopyCommandQueue(this, device.CreateCommandQueue(new SharpDX.Direct3D12.CommandQueueDescription
            {
                Flags = SharpDX.Direct3D12.CommandQueueFlags.None,
                NodeMask = 0,
                Priority = (int)SharpDX.Direct3D12.CommandQueuePriority.Normal,
                Type = SharpDX.Direct3D12.CommandListType.Copy
            }));
        }

        internal bool RootSignature11() 
        {
            SharpDX.Direct3D12.FeatureDataRootSignature data;
            data.HighestVersion = SharpDX.Direct3D12.RootSignatureVersion.Version11;
            if (device.CheckFeatureSupport(SharpDX.Direct3D12.Feature.RootSignature, ref data))
            {
                return true;
            }

            return false;
        }

        public DepthBuffer CreateDepthBuffer(int width, int height)
        {
            return new DepthBuffer(device, depthStencilPool.GetSlot(), width, height);
        }

        public Resource CreateStaticBuffer(int size)
        {
            return new Resource(device.CreateCommittedResource(new SharpDX.Direct3D12.HeapProperties(SharpDX.Direct3D12.HeapType.Default),
                SharpDX.Direct3D12.HeapFlags.None,
                SharpDX.Direct3D12.ResourceDescription.Buffer(new SharpDX.Direct3D12.ResourceAllocationInformation { Alignment = 65536, SizeInBytes = size }),
                SharpDX.Direct3D12.ResourceStates.Common));
        }

        public UploadResource CreateUploadBuffer(int size)
        {
            return new UploadResource(device.CreateCommittedResource(new SharpDX.Direct3D12.HeapProperties(SharpDX.Direct3D12.HeapType.Upload),
                SharpDX.Direct3D12.HeapFlags.None,
                SharpDX.Direct3D12.ResourceDescription.Buffer(new SharpDX.Direct3D12.ResourceAllocationInformation { Alignment = 65536, SizeInBytes = size }),
                SharpDX.Direct3D12.ResourceStates.GenericRead));
        }

        public Pipeline CreatePipeline(PipelineDescriptor descriptor)
        {
            var flags = SharpDX.Direct3D12.RootSignatureFlags.AllowInputAssemblerInputLayout
                | SharpDX.Direct3D12.RootSignatureFlags.DenyHullShaderRootAccess
                | SharpDX.Direct3D12.RootSignatureFlags.DenyDomainShaderRootAccess
                | SharpDX.Direct3D12.RootSignatureFlags.DenyGeometryShaderRootAccess
                | SharpDX.Direct3D12.RootSignatureFlags.DenyPixelShaderRootAccess;

            var parameter = new SharpDX.Direct3D12.RootParameter1(SharpDX.Direct3D12.ShaderVisibility.Vertex, new SharpDX.Direct3D12.RootConstants(0, 0, Marshal.SizeOf<System.Numerics.Matrix4x4>() / 4));
            var signatureDesc = new SharpDX.Direct3D12.RootSignatureDescription1(flags, new[] { parameter });

            var signature = device.CreateRootSignature(signatureDesc.Serialize());
            var desc = new SharpDX.Direct3D12.GraphicsPipelineStateDescription
            {
                PrimitiveTopologyType = SharpDX.Direct3D12.PrimitiveTopologyType.Triangle,
                DepthStencilFormat = SharpDX.DXGI.Format.D32_Float,
                RootSignature = signature,
                PixelShader = new SharpDX.Direct3D12.ShaderBytecode(shaderPool.Get(descriptor.PixelShader, "ps_5_1")),
                VertexShader = new SharpDX.Direct3D12.ShaderBytecode(shaderPool.Get(descriptor.VertexShader, "vs_5_1")),
                RenderTargetCount = descriptor.RenderTargetFormats.Length,
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

            for (int i = 0; i < descriptor.RenderTargetFormats.Length; ++i)
            {
                desc.RenderTargetFormats[i] = descriptor.RenderTargetFormats[i].Format;
            }

            return new Pipeline(device.CreateGraphicsPipelineState(desc), signature);
        }

        public void Dispose()
        {
            device.Dispose();
        }
    }
}
