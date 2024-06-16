namespace Renderer.Direct3D12
{
    internal class Shader
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

        public static ReadOnlyMemory<byte> LoadDxil(string filename, string profile)
        {
            using (var compiler = Vortice.Dxc.Dxc.CreateDxcCompiler<Vortice.Dxc.IDxcCompiler3>())
            using (var result = compiler.Compile(File.ReadAllText(filename), ["-Zi", "-Qembed_debug", $"-T {profile}"], null))
            {
                if (!result.GetStatus().Success)
                {
                    throw new InvalidOperationException(result.GetErrors());
                }
                return result.GetResult().AsMemory();
            }
        }

        private readonly int CP_UTF8 = 65001;
    }
}
