namespace Renderer.Direct3D12.Shaders.Emit
{
    public class Type
    {
        public Dictionary<string, string> Emit(List<CompilationResult> results, List<EmittedFile> files)
        {
            var seen = new Dictionary<string, IHlslType>();

            foreach (var input in results.SelectMany(s => s.Inputs).Where(t => t.Type != null))
            {
                Traverse(input.Type, seen);
            }

            foreach (var type in seen)
            {
                Emit(type.Value, files);
            }

            return seen.Where(x => x.Value is StructHlslType).ToDictionary(x => x.Value.Name, x => CsharpName(x.Value, null));
        }

        private void Traverse(IHlslType type, Dictionary<string, IHlslType> seen)
        {
            if (seen.ContainsKey(type.Name)) return;

            seen.Add(type.Name, type);

            if (type is StructHlslType str)
            {
                foreach (var member in str.Members)
                {
                    Traverse(member.Type, seen);
                }
            }
        }

        private void Emit(IHlslType type, List<EmittedFile> files)
        {
            if (!(type is StructHlslType structure)) return;

            files.Add(new EmittedFile
            {
                RelativePath = $"Shaders/Data/{CsharpName(type, null)}.g.cs",
                Contents = @$"using System.Numerics;
using Data.Space;
using System.Runtime.InteropServices;

namespace Renderer.Direct3D12.Shaders.Data {{
    [StructLayout(LayoutKind.Explicit, Size = {structure.Size})]
    struct {CsharpName(type, null)} {{
{string.Join("\r\n", structure.Members.Select(m => StructureMember(m)))}
    }}
}}"
            });
        }

        private string StructureMember(StructMember member)
        {
            return $@"
        [FieldOffset({member.Offset})]
        public required {CsharpName(member.Type, member.Name)} {member.Name};";
        }

        private string CsharpName(IHlslType type, string? memberName)
        {
            if (type is StructHlslType structure) return structure.Name;
            if (type == PrimitiveHlslType.Float) return "float";
            if (type == PrimitiveHlslType.Uint) return "uint";
            if (type == PrimitiveHlslType.Int) return "int";
            if (type == PrimitiveHlslType.Bool) return "bool";
            if (type is MatrixHlslType matrix)
            {
                return $"Matrix{matrix.Rows}x{matrix.Columns}";
            }
            if (type is VectorHlslType vector && vector.Underlying == PrimitiveHlslType.Float)
            {
                if (memberName.Contains("colour", StringComparison.InvariantCultureIgnoreCase))
                {
                    return "RGB";
                }

                return $"Vector{vector.Elements}";
            }

            throw new InvalidOperationException($"Could not convert HLSL type {type.Name}");
        }
    }
}
