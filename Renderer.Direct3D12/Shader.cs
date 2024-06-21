using SharpGen.Runtime;
using System.Diagnostics;

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
            var path = Path.GetFullPath(filename);
            using (var compiler = Vortice.Dxc.Dxc.CreateDxcCompiler<Vortice.Dxc.IDxcCompiler3>())
            using (var utils = Vortice.Dxc.Dxc.CreateDxcUtils())
            using (var result = compiler.Compile(File.ReadAllText(path), ["-Zi", "-Qembed_debug", $"-T {profile}"], new IncludeHandler(path, utils)))
            {
                if (!result.GetStatus().Success)
                {
                    throw new InvalidOperationException(result.GetErrors());
                }
                return result.GetResult().AsMemory();
            }
        }

        private class IncludeHandler : CallbackBase, Vortice.Dxc.IDxcIncludeHandler
        {
            private readonly string path;
            private readonly Vortice.Dxc.IDxcUtils utils;

            public IncludeHandler(string path, Vortice.Dxc.IDxcUtils utils)
            {
                this.path = path;
                this.utils = utils;
            }

            public void Dispose()
            {
            }

            public Result LoadSource(string filename, out Vortice.Dxc.IDxcBlob includeSource)
            {
                includeSource = null;

                try
                {
                    includeSource = utils.LoadFile(Path.Combine(Path.GetDirectoryName(path), filename), null);
                } 
                catch (Exception ex)
                {
                    Debugger.Break();
                    return Result.Fail;               
                }
                return Result.Ok;
            }
        }

        private readonly int CP_UTF8 = 65001;
    }
}
