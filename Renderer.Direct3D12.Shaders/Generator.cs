using System;
using System.Text.RegularExpressions;

namespace Renderer.Direct3D12.Shaders
{
    internal class Generator
    {
        private const string model = "6_6";
        private readonly Vortice.Dxc.IDxcCompiler3 compiler;
        private readonly Vortice.Dxc.IDxcUtils utils;

        public Generator()
        {
            compiler = Vortice.Dxc.Dxc.CreateDxcCompiler<Vortice.Dxc.IDxcCompiler3>();
            utils = Vortice.Dxc.Dxc.CreateDxcUtils();
        }

        public CompilationResult TryLoad(string filename)
        {
            var text = File.ReadAllText(filename);
            if (text.Contains("[numthreads("))
            {
                return LoadComputeShader(filename, text);
            }

            if (text.Contains("[shader(\""))
            {
                return LoadDxil(filename, text);
            }

            if (filename.EndsWith("Structured.hlsl"))
            {
                // With bindless we can no longer reflect directly on structured buffers. Instead, we will horribly fake it.
                var structNames = text.Split("\r\n", StringSplitOptions.TrimEntries).Where(s => s.StartsWith("struct ")).Select(s => s.Replace("struct ", "")).ToArray();
                var fakeText = $@"#include ""Structured.hlsl""

{string.Join("\r\n", structNames.Select((s, index) => $"StructuredBuffer<{s}> {s}buffer : register(t{index});"))}

[shader(""closesthit"")]
void ClosestHit(inout RadiancePayload payload, TriangleAttributes attrib) {{
{string.Join("\r\n", structNames.Select(s => $"fakeUse(payload, {s}buffer[0]);"))}
}}
";

                var result = LoadDxil(Path.Combine(Path.GetDirectoryName(filename), "fake.hlsl"), fakeText);
                return result with { Type = EmitType.None };
            }

            return null;
        }

        private CompilationResult LoadComputeShader(string filename, string text)
        {
            using (var result = compiler.Compile(text, Arguments("cs", filename, "compute"), utils.CreateDefaultIncludeHandler()))
            {
                if (!result.GetStatus().Success)
                {
                    throw new InvalidOperationException(result.GetErrors());
                }

                using (var output = result.GetOutput(Vortice.Dxc.DxcOutKind.Reflection))
                using (var reflection = utils.CreateReflection<Vortice.Direct3D12.Shader.ID3D12ShaderReflection>(output))
                {
                    return new CompilationResult
                    {
                        DXIL = result.GetResult().AsBytes(),
                        Inputs = Enumerable.Range(0, reflection.Description.BoundResources).Select(bi => Map(new ShaderReflectionContext(reflection), reflection.GetResourceBindingDescription(bi))).ToArray(),
                        Export = "compute",
                        Path = filename,
                        Type = EmitType.Compute,
                    };
                }
            }
        }

        private CompilationResult LoadDxil(string filename, string text)
        {
            using (var result = compiler.Compile(text, Arguments("lib", filename, null), utils.CreateDefaultIncludeHandler()))
            {
                if (!result.GetStatus().Success)
                {
                    throw new InvalidOperationException(result.GetErrors());
                }

                using (var output = result.GetOutput(Vortice.Dxc.DxcOutKind.Reflection))
                using (var reflection = utils.CreateReflection<Vortice.Direct3D12.Shader.ID3D12LibraryReflection>(output))
                {
                    var functions = Enumerable.Range(0, reflection.Description.FunctionCount).Select(x => reflection.GetFunctionByIndex(x)).Where(s => !s.Description.Name.StartsWith("_GLOBAL")).ToArray();
                    if (functions.Length != 1) throw new InvalidOperationException($"Found {reflection.Description.FunctionCount} entry points in {filename}: {string.Join(", ", functions.Select(s => s.Description.Name))}");

                    var function = functions[0];
                    return new CompilationResult
                    {
                        DXIL = result.GetResult().AsBytes(),
                        Inputs = Enumerable.Range(0, reflection.Description.FunctionCount).Select(f => reflection.GetFunctionByIndex(f)).SelectMany(function => Enumerable.Range(0, function.Description.BoundResources).Select(bi => Map(new FunctionReflectionContext(function), function.GetResourceBindingDescription(bi)))).ToArray(),
                        Export = UnmangleName(function.Description.Name),
                        Path = filename,
                        Type = EmitType.DXR,
                    };
                }
            }
        }

        private string UnmangleName(string name)
        {
            return new Regex("\\w+").Matches(name)[0].Value;
        }

        private BoundInput Map(IReflectionContext context, Vortice.Direct3D12.Shader.InputBindingDescription desc)
        {
            if (desc.Type == Vortice.Direct3D.ShaderInputType.ConstantBuffer)
            {
                var variable = context.GetVariableByName(desc.Name);
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
                var buffer = context.GetConstantBufferByName(desc.Name);
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

            if (desc.Type == Vortice.Direct3D.ShaderInputType.Texture)
            {
                return new BoundInput
                {
                    ItemSize = 0,
                    RegisterSpace = desc.Space,
                    Name = desc.Name,
                    RegisterValue = desc.BindPoint,
                    BindType = BindType.Texture,
                    Type = null
                };
            }

            throw new InvalidOperationException();
        }

        private IHlslType Map(Vortice.Direct3D12.Shader.ID3D12ShaderReflectionType type, int? size)
        {
            var name = type.Description.Name;

            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Scalar && type.Description.Type == Vortice.Direct3D.ShaderVariableType.UInt) return PrimitiveHlslType.Uint;
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Scalar && type.Description.Type == Vortice.Direct3D.ShaderVariableType.Float) return PrimitiveHlslType.Float;
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Scalar && type.Description.Type == Vortice.Direct3D.ShaderVariableType.Int) return PrimitiveHlslType.Int;
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Scalar && type.Description.Type == Vortice.Direct3D.ShaderVariableType.Bool) return PrimitiveHlslType.Bool;
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Scalar && type.Description.Type == Vortice.Direct3D.ShaderVariableType.Float16) return PrimitiveHlslType.Half;
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Scalar && type.Description.Type == Vortice.Direct3D.ShaderVariableType.Void) return PrimitiveHlslType.Uint;
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Scalar && type.Description.Type == Vortice.Direct3D.ShaderVariableType.UInt16) return PrimitiveHlslType.Ushort;

            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.MatrixColumns && type.Description.Type == Vortice.Direct3D.ShaderVariableType.Float)
            {
                return new MatrixHlslType { Columns = type.Description.ColumnCount, Rows = type.Description.RowCount, Underlying = PrimitiveHlslType.Float };
            }
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Vector && type.Description.Type == Vortice.Direct3D.ShaderVariableType.Float)
            {
                return new VectorHlslType
                {
                    Underlying = PrimitiveHlslType.Float,
                    Elements = type.Description.ColumnCount
                };
            }
            if (type.Description.Class == Vortice.Direct3D.ShaderVariableClass.Vector && type.Description.Type == Vortice.Direct3D.ShaderVariableType.Float16)
            {
                return new VectorHlslType
                {
                    Underlying = PrimitiveHlslType.Half,
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
                            Type = Map(type.GetMemberTypeByIndex(x), MemberSize(type, size.Value, x)),
                            Name = type.GetMemberTypeName(x),
                            Offset = type.GetMemberTypeByIndex(x).Description.Offset,
                        })
                        .ToArray()
                };
            }

            throw new InvalidOperationException($"Could not load type {type.Description.Name}");
        }

        private int MemberSize(Vortice.Direct3D12.Shader.ID3D12ShaderReflectionType type, int parentSize, int memberIndex)
        {
            if (type.Description.MemberCount == memberIndex + 1)
            {
                return parentSize - type.GetMemberTypeByIndex(memberIndex).Description.Offset;
            }

            return type.GetMemberTypeByIndex(memberIndex + 1).Description.Offset - type.GetMemberTypeByIndex(memberIndex).Description.Offset;
        }

        private string[] Arguments(string type, string filename, string entryPoint)
        {
            var args = new List<string> { filename };
#if DEBUG
            args.Add("-Zi");
            args.Add("-Qembed_debug");
//            args.Add("-Od");
#endif
            //args.Add("-Zpr"); // Row-major matrices
            args.Add($"-T {type}_{model}");
            if (entryPoint != null)
            {
                args.Add($"-E {entryPoint}");
            }
            args.Add("-enable-16bit-types");

            return args.ToArray();
        }

        private readonly int CP_UTF8 = 65001;

        private interface IReflectionContext
        {
            Vortice.Direct3D12.Shader.ID3D12ShaderReflectionVariable GetVariableByName(string name);
            Vortice.Direct3D12.Shader.ID3D12ShaderReflectionConstantBuffer GetConstantBufferByName(string name);
        }

        private class ShaderReflectionContext : IReflectionContext
        {
            private readonly Vortice.Direct3D12.Shader.ID3D12ShaderReflection shader;

            public ShaderReflectionContext(Vortice.Direct3D12.Shader.ID3D12ShaderReflection shader)
            {
                this.shader = shader;
            }

            public Vortice.Direct3D12.Shader.ID3D12ShaderReflectionConstantBuffer GetConstantBufferByName(string name)
                => shader.GetConstantBufferByName(name);
            public Vortice.Direct3D12.Shader.ID3D12ShaderReflectionVariable GetVariableByName(string name)
                => shader.GetVariableByName(name);
        }

        private class FunctionReflectionContext : IReflectionContext
        {
            private readonly Vortice.Direct3D12.Shader.ID3D12FunctionReflection func;

            public FunctionReflectionContext(Vortice.Direct3D12.Shader.ID3D12FunctionReflection func)
            {
                this.func = func;
            }

            public Vortice.Direct3D12.Shader.ID3D12ShaderReflectionConstantBuffer GetConstantBufferByName(string name)
                => func.GetConstantBufferByName(name);
            public Vortice.Direct3D12.Shader.ID3D12ShaderReflectionVariable GetVariableByName(string name)
                => func.GetVariableByName(name);
        }
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
        Texture,
    }

    public record class CompilationResult
    {
        public required BoundInput[] Inputs { get; init; }
        public required byte[] DXIL { get; init; }
        public required string Export { get; init; }
        public required string Path { get; init; }
        public required EmitType Type { get; init; }
    }

    public enum EmitType
    {
        DXR,
        Compute,
        None,
    }
}
