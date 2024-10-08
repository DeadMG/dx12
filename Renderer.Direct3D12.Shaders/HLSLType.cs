﻿namespace Renderer.Direct3D12.Shaders
{
    public interface IHlslType
    {
        public string Name { get; }
    }

    public class PrimitiveHlslType : IHlslType
    {
        public required string Name { get; init; }

        public static readonly PrimitiveHlslType Uint = new PrimitiveHlslType { Name = "uint" };
        public static readonly PrimitiveHlslType Float = new PrimitiveHlslType { Name = "float" };
        public static readonly PrimitiveHlslType Int = new PrimitiveHlslType { Name = "int" };
        public static readonly PrimitiveHlslType Bool = new PrimitiveHlslType { Name = "bool" };
        public static readonly PrimitiveHlslType Half = new PrimitiveHlslType { Name = "half" };
        public static readonly PrimitiveHlslType Ushort = new PrimitiveHlslType { Name = "ushort" };
    }

    public class StructHlslType : IHlslType
    {
        public required uint Size { get; init; }
        public required string Name { get; init; }
        public required StructMember[] Members { get; init; }
    }

    public class StructMember
    {
        public required string Name { get; init; }
        public required uint Offset { get; init; }
        public required IHlslType Type { get; init; }
    }

    public class VectorHlslType : IHlslType
    {
        public required IHlslType Underlying { get; init; }
        public required uint Elements { get; init; }

        public string Name => $"{Underlying.Name}x{Elements}";
    }

    public class MatrixHlslType : IHlslType
    {
        public required IHlslType Underlying { get; init; }
        public required uint Rows { get; init; }
        public required uint Columns { get; init; }
        public string Name => $"{Underlying.Name}x{Rows}x{Columns}";
    }

    public class RaytraceStructure : IHlslType
    {
        public string Name => $"RaytracingAccelerationStructure";
    }
}
