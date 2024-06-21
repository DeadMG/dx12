namespace Renderer.Direct3D12.Shaders.ShaderModel
{
    public class HLSLType
    {
        public required string Name { get; init; }
        public required Member[] Members { get; init; }
        public required int Rows { get; init; }
        public required int Cols { get; init; }
        public required int Elements { get; init; }
        public required int Offset { get; init; }
        public required TypeCategory Category { get; init; }
    }

    public class Member
    {
        public required HLSLType Type { get; init; }
        public required string Name { get; init; }
    }

    public enum TypeCategory
    {
        Scalar,
        Vector,
        MatrixRows,
        MatrixColumns,
        Object,
        Struct,
        InterfaceClass,
        InterfacePointer,
    }

    public enum TypeType
    {

    }
}
