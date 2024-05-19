namespace Wrapper.Direct3D
{
    public class PipelineDescriptor
    {
        public Shader VertexShader { get; set; }
        public Shader PixelShader { get; set; }

        public RenderTargetFormat[] RenderTargetFormats { get; set; }
    }
}
