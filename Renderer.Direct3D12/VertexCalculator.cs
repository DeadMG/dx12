using Simulation;
using System.Numerics;

namespace Renderer.Direct3D12
{
    internal class VertexCalculator
    {
        public ComputedVertex[] CalculateVertices(Mesh mesh)
        {
            var normals = new List<Vector3>(mesh.Vertices.Length);
            normals.AddRange(Enumerable.Repeat(new Vector3(0, 0, 0), mesh.Vertices.Length));

            foreach (var chunk in mesh.Indices.Chunk(3))
            {
                var a = mesh.Vertices[chunk[0]].Position;
                var b = mesh.Vertices[chunk[1]].Position;
                var c = mesh.Vertices[chunk[2]].Position;

                var area = 0.5f * Vector3.Cross(-(a - b), a - c).Length();

                var weightedNormal = Plane.CreateFromVertices(a, b, c).Normal * (float)area;

                normals[(int)chunk[0]] += weightedNormal;
                normals[(int)chunk[1]] += weightedNormal;
                normals[(int)chunk[2]] += weightedNormal;
            }

            return mesh.Vertices
                .Select((v, index) => new ComputedVertex
                {
                    Colour = v.Colour,
                    Position = v.Position,
                    Normal = Vector3.Normalize(normals[index])
                })
                .ToArray();
        }
    }
}
