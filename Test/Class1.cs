using System.Numerics;
using Xunit;

namespace Test
{
    public class Class1
    {
        [Fact]
        public void PointsOnPlane()
        {
            var normal = new Vector3(-0.00243024598f, 0.779688179f, -0.626163304f);

            var a = FromPIX(new PIX { x = -6.48995161, y = 0.758145332, z = 25.5721493 });
            var b = FromPIX(new PIX { x = -5.48940229, y = 0.495149612, z = 22.7472420 });
            var c = FromPIX(new PIX { x = -7.52244329, y = 2.28324032, z = 21.6662369 });

            var computedNormal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            computedNormal.Z = -computedNormal.Z;

            Assert.Equal(computedNormal, normal);
        }

        public static Plane CreateFromArbitraryVertexAndNormal(Vector3 vertex, Vector3 normal)
        {
            return new(normal, Vector3.Dot(normal, vertex));
        }

        private static Vector3 FromPIX(PIX p)
        {
            return new Vector3((float)p.x, (float)p.y, (float)p.z);
        }

        private struct PIX
        {
            public double x;
            public double y;
            public double z;
        }
    }

    // 

}
