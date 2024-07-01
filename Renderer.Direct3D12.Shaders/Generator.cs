using SharpGen.Runtime;

namespace Renderer.Direct3D12.Shaders
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
                    if (reflection.Description.FunctionCount != 1) throw new InvalidOperationException();

                    var function = reflection.GetFunctionByIndex(0);

                    return new CompilationResult
                    {
                        DXIL = result.GetResult().AsBytes(),
                        Inputs = Enumerable.Range(0, function.Description.BoundResources).Select(bi => Map(function, function.GetResourceBindingDescription(bi))).ToArray(),
                        Export = function.Description.Name,
                        Path = filename,
                    };
                }
            }
        }

        private BoundInput Map(Vortice.Direct3D12.Shader.ID3D12FunctionReflection func, Vortice.Direct3D12.Shader.InputBindingDescription desc)
        {
            if (desc.Type == Vortice.Direct3D.ShaderInputType.ConstantBuffer)
            {
                var variable = func.GetVariableByName(desc.Name);
                return new BoundInput
                {
                    ItemSize = variable.Description.Size,
                    RegisterSpace = desc.Space,
                    Name = desc.Name,
                    RegisterValue = desc.BindPoint,
                    BindType = BindType.ConstantBuffer,
                    Type = Map(variable.VariableType, variable.Description.Size)
                };
            }

            if (desc.Type == Vortice.Direct3D.ShaderInputType.Structured)
            {
                var buffer = func.GetConstantBufferByName(desc.Name);
                var variable = buffer.GetVariableByIndex(0);
                return new BoundInput
                {
                    ItemSize = desc.NumSamples,
                    RegisterSpace = desc.Space,
                    Name = desc.Name,
                    RegisterValue = desc.BindPoint,
                    BindType = BindType.StructuredBuffer,
                    Type = Map(variable.VariableType, variable.Description.Size)
                };
            }

            if (desc.Type == Vortice.Direct3D.ShaderInputType.Rtaccelerationstructure)
            {
                return new BoundInput
                {
                    ItemSize = 0,
                    RegisterSpace = desc.Space,
                    Name = desc.Name,
                    RegisterValue = desc.BindPoint,
                    BindType = BindType.RtAccelerationStructure,
                    Type = null
                };
            }

            if (desc.Type == Vortice.Direct3D.ShaderInputType.UnorderedAccessViewRWTyped)
            {
                return new BoundInput
                {
                    ItemSize = 0,
                    RegisterSpace = desc.Space,
                    Name = desc.Name,
                    RegisterValue = desc.BindPoint,
                    BindType = BindType.RWUnorderedAccess,
                    Type = null
                };
            }

            throw new InvalidOperationException();
        }

        private IHlslType Map(Vortice.Direct3D12.Shader.ID3D12ShaderReflectionType type, int? size)
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
                    Name = type.Description.Name,
                    Size = size.Value,
                    Members = Enumerable.Range(0, type.Description.MemberCount)
                        .Select(x => new StructMember
                        {
                            Type = Map(type.GetMemberTypeByIndex(x), null),
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

    public class BoundInput
    {
        public required int RegisterValue { get; init; }
        public required int RegisterSpace { get; init; }
        public required BindType BindType { get; init; }
        public required IHlslType? Type { get; init; }
        public required string Name { get; init; }
        public required int ItemSize { get; init; }
    }

    public enum BindType
    {
        StructuredBuffer,
        ConstantBuffer,
        RtAccelerationStructure,
        RWUnorderedAccess,
    }

    public class CompilationResult
    {
        public required BoundInput[] Inputs { get; init; }
        public required byte[] DXIL { get; init; }
        public required string Export { get; init; }
        public required string Path { get; init; }
    }
}
