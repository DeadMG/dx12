using System.Numerics;

namespace Data.Mesh
{
    public class Mesh : IGeometry
    {
        public required string Name { get; init; }
        public required Vertex[] Vertices { get; init; }
        public required Triangle[] Triangles { get; init; }
        public required Material[] Materials { get; init; }

        public static Mesh NewFromPoints(string name, Vector3[] points, Triangle[] triangles, Material[] materials)
        {
            return new Mesh
            {
                Name = name, 
                Materials = materials,
                Triangles = triangles, 
                Vertices = Vertex.FromPoints(points, triangles) 
            };
        }

        public static readonly Vector3 Facing = new Vector3(0, 0, 1);
    }
}
