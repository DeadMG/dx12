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
            using (var result = compiler.Compile(File.ReadAllText(filename), Arguments(), null))
            {
                if (!result.GetStatus().Success)
                {
                    throw new InvalidOperationException(result.GetErrors());
                }

                using (var reflection = utils.CreateReflection<Vortice.Direct3D12.Shader.ID3D12ShaderReflection>(result.GetOutput(Vortice.Dxc.DxcOutKind.Reflection)))
                {
                    return new CompilationResult
                    {
                        DXIL = result.GetResult().AsBytes(),
                        ConstantBuffers = reflection.ConstantBuffers.Select(Map).ToArray()
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
                StartSampler = variable.Description.StartSampler,
                StartTexture = variable.Description.StartTexture,
                StartOffset = variable.Description.StartOffset,
                SamplerSize = variable.Description.SamplerSize,
                TextureSize = variable.Description.TextureSize,

                Size = variable.Description.Size,
                Name = variable.Description.Name,
                Type = Map(variable.VariableType) 
            };
        }

        private HLSLType Map(Vortice.Direct3D12.Shader.ID3D12ShaderReflectionType type)
        {
            return new HLSLType 
            {
                Rows = type.Description.RowCount,
                Cols = type.Description.ColumnCount,
                Elements = type.Description.ElementCount,
                Name = type.Description.Name,
                Offset = type.Description.Offset,
                Category = Map(type.Description.Class),
                
                Members = Enumerable.Range(0, type.Description.MemberCount)
                    .Select(x => new Member
                    {
                        Type = Map(type.GetMemberTypeByIndex(x)),
                        Name = type.GetMemberTypeName(x)
                    })
                    .ToArray()
            };
        }

        private TypeCategory Map(Vortice.Direct3D.ShaderVariableClass type)
        {
            if (type == Vortice.Direct3D.ShaderVariableClass.Scalar) return TypeCategory.Scalar;
            if (type == Vortice.Direct3D.ShaderVariableClass.Vector) return TypeCategory.Vector;
            if (type == Vortice.Direct3D.ShaderVariableClass.MatrixRows) return TypeCategory.MatrixRows;
            if (type == Vortice.Direct3D.ShaderVariableClass.MatrixColumns) return TypeCategory.MatrixColumns;
            if (type == Vortice.Direct3D.ShaderVariableClass.Object) return TypeCategory.Object;
            if (type == Vortice.Direct3D.ShaderVariableClass.Struct) return TypeCategory.Struct;
            if (type == Vortice.Direct3D.ShaderVariableClass.InterfaceClass) return TypeCategory.InterfaceClass;
            return TypeCategory.InterfacePointer;
        }

        private string[] Arguments()
        {
            var args = new List<string>();
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
