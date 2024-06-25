namespace Data.Mesh
{
    public struct Triangle
    {
        public required int[] Vertices { get; init; }
        public required byte MaterialIndex { get; init; }
    }
}
