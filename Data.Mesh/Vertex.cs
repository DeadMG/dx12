using System.Numerics;

namespace Data.Mesh
{
    public struct Vertex
    {
        public required Vector3 Position { get; init; }
        public required Vector3 Normal { get; init; }

        public static Vertex[] FromPoints(Vector3[] points, Triangle[] triangles)
        {
            var normals = new List<Vector3>(points.Length);
            normals.AddRange(Enumerable.Repeat(new Vector3(0, 0, 0), points.Length));

            foreach (var triangle in triangles)
            {
                var a = points[triangle.Vertices[0]];
                var b = points[triangle.Vertices[1]];
                var c = points[triangle.Vertices[2]];

                var weightedNormal = Vector3.Cross(b - a, c - a);

                normals[triangle.Vertices[0]] += weightedNormal;
                normals[triangle.Vertices[1]] += weightedNormal;
                normals[triangle.Vertices[2]] += weightedNormal;

                var normalNormal = Vector3.Normalize(weightedNormal);

            }

            var offset = points.Aggregate(new Vector3(0,0,0), (a, b) => a + b) / points.Length;

            return points
                .Select((v, index) => new Vertex
                {
                    Position = v - offset,
                    Normal = Vector3.Normalize(normals[index])
                })
                .ToArray();
        }
    }
}
