using System.Numerics;

namespace Data.Mesh
{
    public class IcosphereGenerator
    {
        const float X = .525731112119133606f;
        const float Z = .850650808352039932f;
        const float N = 0.0f;

        static readonly IReadOnlyList<Vector3> baseVertices = new List<Vector3>
        {
            new Vector3(-X,N,Z),
            new Vector3(X,N,Z),
            new Vector3(-X,N,-Z),
            new Vector3(X,N,-Z),
            new Vector3(N,Z,X),
            new Vector3(N,Z,-X),
            new Vector3(N,-Z,X),
            new Vector3(N,-Z,-X),
            new Vector3(Z, X,N),
            new Vector3(-Z,X, N),
            new Vector3(Z,-X,N),
            new Vector3(-Z,-X, N)
        };

        static readonly IReadOnlyList<Triangle> baseTriangles = new List<Triangle>
        {
            new Triangle { MaterialIndex = 0, Vertices = [0,4,1] },
            new Triangle { MaterialIndex = 0, Vertices = [0,9,4] },
            new Triangle { MaterialIndex = 0, Vertices = [9,5,4] },
            new Triangle { MaterialIndex = 0, Vertices = [4,5,8] },
            new Triangle { MaterialIndex = 0, Vertices = [4,8,1] },
            new Triangle { MaterialIndex = 0, Vertices = [8,10,1] },
            new Triangle { MaterialIndex = 0, Vertices = [8,3,10] },
            new Triangle { MaterialIndex = 0, Vertices = [5,3,8] },
            new Triangle { MaterialIndex = 0, Vertices = [5,2,3] },
            new Triangle { MaterialIndex = 0, Vertices = [2,7,3] },
            new Triangle { MaterialIndex = 0, Vertices = [7,10,3] },
            new Triangle { MaterialIndex = 0, Vertices = [7,6,10] },
            new Triangle { MaterialIndex = 0, Vertices = [7,11,6] },
            new Triangle { MaterialIndex = 0, Vertices = [11,0,6] },
            new Triangle { MaterialIndex = 0, Vertices = [0,1,6] },
            new Triangle { MaterialIndex = 0, Vertices = [6,1,10] },
            new Triangle { MaterialIndex = 0, Vertices = [9,0,11] },
            new Triangle { MaterialIndex = 0, Vertices = [9,11,2] },
            new Triangle { MaterialIndex = 0, Vertices = [9,2,5] },
            new Triangle { MaterialIndex = 0, Vertices = [7,2,11] }
        };

        struct IndexPair
        {
            public int First;
            public int Second;

            public override bool Equals(object? obj)
            {
                if (obj is IndexPair op)
                {
                    return Equals(op);
                }

                return false;
            }

            public bool Equals(IndexPair other)
            {
                return Math.Min(First, Second) == Math.Min(other.First, other.Second) && Math.Max(First, Second) == Math.Max(other.First, other.Second);
            }

            public override int GetHashCode()
            {
                return (Math.Min(First, Second) * 17) + Math.Max(First, Second);
            }
        }

        public Mesh Generate(int subdivisions, Material material)
        {
            var vertices = new List<Vector3>(baseVertices.Select(v => Vector3.Normalize(v)));
            var triangles = new List<Triangle>(baseTriangles);

            for (int i = 0; i < subdivisions; i++)
            {
                triangles = Subdivide(vertices, triangles);
            }

            return Mesh.NewFromPoints($"Icosphere {subdivisions}", vertices.ToArray(), triangles.ToArray(), [material]);
        }

        private List<Triangle> Subdivide(List<Vector3> vertices, List<Triangle> triangles)
        {
            var lookup = new Dictionary<IndexPair, int>();
            var result = new List<Triangle>();

            foreach (var tri in triangles)
            {
                var mid = new int[3];
                for (int edge = 0; edge < 3; edge++)
                {
                    mid[edge] = VertexForEdge(lookup, vertices, tri.Vertices[edge], tri.Vertices[(edge + 1) % 3]);
                }

                result.Add(new Triangle { MaterialIndex = 0, Vertices = [tri.Vertices[0], mid[0], mid[2]] });
                result.Add(new Triangle { MaterialIndex = 0, Vertices = [tri.Vertices[1], mid[1], mid[0]] });
                result.Add(new Triangle { MaterialIndex = 0, Vertices = [tri.Vertices[2], mid[2], mid[1]] });
                result.Add(new Triangle { MaterialIndex = 0, Vertices = [mid[0], mid[1], mid[2]] });
            }

            return result;
        }

        private int VertexForEdge(Dictionary<IndexPair, int> lookup, List<Vector3> vertices, int first, int second)
        {
            var pair = new IndexPair { First = first, Second = second };
            if (lookup.ContainsKey(pair)) return lookup[pair];

            var newVert = Vector3.Normalize(vertices[first] + vertices[second]);
            var newIndex = vertices.Count;
            lookup[pair] = newIndex;
            vertices.Add(newVert);

            return newIndex;
        }
    }
}
