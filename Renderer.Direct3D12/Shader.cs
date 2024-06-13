namespace Renderer.Direct3D12
{
    public class Shader
    {
        public static ReadOnlyMemory<byte> Load(string filename, string entryPoint, string profile)
        {
            return Vortice.D3DCompiler.Compiler.CompileFromFile(
                filename,
                entryPoint,
                profile,
                Vortice.D3DCompiler.ShaderFlags.None,
                Vortice.D3DCompiler.EffectFlags.None);
        }
    }
}
