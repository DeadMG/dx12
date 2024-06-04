namespace Renderer.Direct3D12
{
    public class Shader
    {
        public static SharpDX.Direct3D12.ShaderBytecode Load(string filename, string entryPoint, string profile)
        {
            using (var bytecode = SharpDX.D3DCompiler.ShaderBytecode.Compile(
                File.ReadAllText(filename),
                entryPoint,
                profile,
                SharpDX.D3DCompiler.ShaderFlags.None,
                SharpDX.D3DCompiler.EffectFlags.None,
                filename))
            {
                return new SharpDX.Direct3D12.ShaderBytecode(bytecode);
            }
        }
    }
}
