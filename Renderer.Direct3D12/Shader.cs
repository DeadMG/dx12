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

        public static ReadOnlyMemory<byte> LoadDxil(string filename, string profile, string? entryPoint = null)
        {
            var arguments = new List<string>
            {
                filename,
                "-Zi",
                "-Qembed_debug",
                $"-T {profile}",
#if DEBUG
                "-Od",
#endif
            };

            if (entryPoint != null)
            {
                arguments.Add($"-E {entryPoint}");
            }

            var path = Path.GetFullPath(filename);
            using (var compiler = Vortice.Dxc.Dxc.CreateDxcCompiler<Vortice.Dxc.IDxcCompiler3>())
            using (var utils = Vortice.Dxc.Dxc.CreateDxcUtils())
            using (var result = compiler.Compile(File.ReadAllText(path), arguments.ToArray(), new IncludeHandler(path, utils)))
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

            public Result LoadSource(string filename, out Vortice.Dxc.IDxcBlob includeSource)
            {
                includeSource = null;

                try
                {
                    includeSource = utils.LoadFile(filename, null);
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
