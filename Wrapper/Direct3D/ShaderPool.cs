using System.Collections.Concurrent;

namespace Wrapper.Direct3D
{
    internal class ShaderPool : IDisposable
    {
        private readonly ConcurrentDictionary<string, SharpDX.D3DCompiler.ShaderBytecode> shaders = new ConcurrentDictionary<string, SharpDX.D3DCompiler.ShaderBytecode>();

        public SharpDX.D3DCompiler.ShaderBytecode Get(Shader shader, string profile)
        {
            return shaders.GetOrAdd(shader.Filename, f => SharpDX.D3DCompiler.ShaderBytecode.Compile(
                File.ReadAllText(shader.Filename),
                shader.EntryPoint,
                profile,
                SharpDX.D3DCompiler.ShaderFlags.None,
                SharpDX.D3DCompiler.EffectFlags.None,
                shader.Filename));
        }

        public void Dispose()
        {
            foreach (var kvp in shaders)
            {
                kvp.Value.Dispose();
            }
        }
    }
}
