using SharpGen.Runtime;

namespace Renderer.Direct3D12.Shaders.ShaderModel
{
    internal class Generator
    {
        private readonly Vortice.Dxc.IDxcCompiler3 compiler;
        private readonly Vortice.Dxc.IDxcUtils utils;

        public Generator()
        {
            compiler = Vortice.Dxc.Dxc.CreateDxcCompiler<Vortice.Dxc.IDxcCompiler3>();
            utils = Vortice.Dxc.Dxc.CreateDxcUtils();
        }

        public CompilationResult LoadDxil(string filename)
        {
            using (var result = compiler.Compile(File.ReadAllText(filename), Arguments(filename), utils.CreateDefaultIncludeHandler()))
            {
                if (!result.GetStatus().Success)
                {
                    throw new InvalidOperationException(result.GetErrors());
                }

                using (var output = result.GetOutput(Vortice.Dxc.DxcOutKind.Reflection))
                using (var reflection = utils.CreateReflection<Vortice.Direct3D12.Shader.ID3D12LibraryReflection>(output))
                {
                    var functions = Enumerable.Range(0, reflection.Description.FunctionCount).Select(i => reflection.GetFunctionByIndex(i)).ToArray();
                    return new CompilationResult
                    {
                        DXIL = result.GetResult().AsBytes(),
                        ConstantBuffers = functions.SelectMany(x => Enumerable.Range(0, x.Description.ConstantBuffers).Select(cb => Map(x.GetConstantBufferByIndex(cb)))).ToArray()
                    };
                }
            }
        }

        private ConstantBuffer Map(Vortice.Direct3D12.Shader.ID3D12ShaderReflectionConstantBuffer buffer)
        {
            return new ConstantBuffer
            {
                IsUserPacked = buffer.Description.Flags.HasFlag(Vortice.Direct3D.ConstantBufferFlags.CbfUserpacked),
                Name = buffer.Description.Name,
                Size = buffer.Description.Size,
                BufferType = Map(buffer.Description.Type),
                Variables = buffer.Variables.Select(v => Map(v)).ToArray(),
            };
        }

        private ConstantBufferType Map(Vortice.Direct3D.ConstantBufferType type)
        {
            if (type == Vortice.Direct3D.ConstantBufferType.ConstantBuffer) return ConstantBufferType.ConstantBuffer;
            if (type == Vortice.Direct3D.ConstantBufferType.TextureBuffer) return ConstantBufferType.TextureBuffer;
            if (type == Vortice.Direct3D.ConstantBufferType.InterfacePointers) return ConstantBufferType.InterfacePointers;
            return ConstantBufferType.ResourceBindInfo;
        }

        private Variable Map(Vortice.Direct3D12.Shader.ID3D12ShaderReflectionVariable variable)
        {
            return new Variable 
            {
                StartOffset = variable.Description.StartOffset,

                Size = variable.Description.Size,
                Name = variable.Description.Name,
                Type = Map(variable.VariableType, variable.Description.Size) 
            };
        }

        private IHlslType Map(Vortice.Direct3D12.Shader.ID3D12ShaderReflectionType type, int size = 0)
        {
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Scalar && type.Description.Type == Vortice.Direct3D.ShaderVariableType.UInt) return PrimitiveHlslType.Uint;
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Scalar && type.Description.Type == Vortice.Direct3D.ShaderVariableType.Float) return PrimitiveHlslType.Float;
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Vector && type.Description.Type == Vortice.Direct3D.ShaderVariableType.Float)
            {
                return new VectorHlslType
                {
                    Underlying = PrimitiveHlslType.Float,
                    Elements = type.Description.ColumnCount
                };
            }
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Struct)
            {
                return new StructHlslType
                {
                    Size = size,
                    Name = type.Description.Name,

                    Members = Enumerable.Range(0, type.Description.MemberCount)
                        .Select(x => new StructMember
                        {
                            Type = Map(type.GetMemberTypeByIndex(x)),
                            Name = type.GetMemberTypeName(x),
                            Offset = type.GetMemberTypeByIndex(x).Description.Offset,
                        })
                        .ToArray()
                };
            }

            throw new InvalidOperationException();

        }

        private string[] Arguments(string filename)
        {
            var args = new List<string> { filename };
#if DEBUG
            args.Add("-Zi");
            args.Add("-Qembed_debug");
#endif
            args.Add($"-T lib_6_3");

            return args.ToArray();
        }

        private readonly int CP_UTF8 = 65001;
    }
}
