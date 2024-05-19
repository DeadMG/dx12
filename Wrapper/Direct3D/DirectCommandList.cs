using SharpDX.Mathematics.Interop;
using System.Runtime.InteropServices;

namespace Wrapper.Direct3D
{
    public class DirectCommandList : CommandList
    {
        internal DirectCommandList(CommandQueue queue) : base(queue)
        {
        }

        public void Clear(RenderTargetView rtv, RawColor4 color)
        {
            List.ClearRenderTargetView(rtv.Handle, color);
        }

        public void ClearDepthBuffer(DepthBuffer buffer, float value)
        {
            List.ClearDepthStencilView(buffer.Handle, SharpDX.Direct3D12.ClearFlags.FlagsDepth, value, 0);
        }

        public void SetPipeline(Pipeline pipeline)
        {
            List.PipelineState = pipeline.State;
            List.SetGraphicsRootSignature(pipeline.RootSignature);
        }

        public void SetGraphicsRoot32BitConstants<T>(T value)
            where T : unmanaged
        {
            unsafe
            {
                List.SetGraphicsRoot32BitConstants(0, Marshal.SizeOf<T>() / 4, new IntPtr(&value), 0);
            }
        }

        public new SharpDX.Direct3D12.GraphicsCommandList List => base.List;
    }
}
