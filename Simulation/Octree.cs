using Data;
using System.Numerics;

namespace Simulation
{
    public class Octree
    {
        private readonly OctreeNode rootNode;

        public Octree(IEnumerable<Unit> units, Vector3 volume)
        {
            rootNode = new OctreeNode(new AABB { Start = -volume / 2, End = volume / 2 });

            foreach (var unit in units)
            {
                rootNode.Insert(unit);
            }
        }

        public void Intersect(HashSet<Unit> results, Frustum frustum) => rootNode.Intersect(results, frustum);

        public HashSet<Unit> Intersect(Ray ray)
        {
            var results = new HashSet<Unit>();
            rootNode.Intersect(results, ray);
            return results;
        }

        private struct OctreeNode
        {
            private readonly AABB boundingVolume;
            private readonly HashSet<Unit> units = new HashSet<Unit>();
            private OctreeNode[]? childNodes;

            public OctreeNode(AABB boundingVolume)
            {
                this.boundingVolume = boundingVolume;
            }

            public void Intersect(HashSet<Unit> results, Ray ray)
            {
                if (!Collision.Intersect(boundingVolume, ray)) return;

                foreach (var unit in units)
                {
                    if (Collision.Intersect(unit.BoundingBox, ray))
                    {
                        results.Add(unit);
                    }
                }

                if (childNodes == null) return;

                foreach (var child in childNodes)
                {
                    child.Intersect(results, ray);
                }
            }

            public void Intersect(HashSet<Unit> results, Frustum frustum)
            {
                if (!Collision.HSTIntersect(boundingVolume, frustum)) return;

                foreach (var unit in units)
                {
                    if (Collision.HSTIntersect(unit.BoundingBox, frustum))
                    {
                        results.Add(unit);
                    }
                }

                if (childNodes == null) return;

                foreach (var child in childNodes)
                {
                    child.Intersect(results, frustum);
                }
            }

            public void Insert(Unit unit)
            {
                // We may keep it here just to terminate the tree if no further subdivision is needed
                if (childNodes == null)
                {
                    if (units.Count <= 7)
                    {
                        units.Add(unit);
                        return;
                    }

                    var center = boundingVolume.Center();
                    childNodes = new OctreeNode[]
                    {
                        new OctreeNode(new AABB
                        {
                            Start = boundingVolume.Start,
                            End = center,
                        }),
                        new OctreeNode(new AABB
                        {
                            Start = new Vector3(center.X, boundingVolume.Start.Y, boundingVolume.Start.Z),
                            End = new Vector3(boundingVolume.End.X, center.Y, center.Z),
                        }),
                        new OctreeNode(new AABB
                        {
                            Start = new Vector3(boundingVolume.Start.X, center.Y, boundingVolume.Start.Z),
                            End = new Vector3(center.X, boundingVolume.End.Y, center.Z),
                        }),
                        new OctreeNode(new AABB
                        {
                            Start = new Vector3(boundingVolume.Start.X, boundingVolume.Start.Y, center.Z),
                            End = new Vector3(center.X, center.Y, boundingVolume.End.Z),
                        }),
                        new OctreeNode(new AABB
                        {
                            Start = new Vector3(boundingVolume.Start.X, center.Y, center.Z),
                            End = new Vector3(center.X, boundingVolume.End.Y, boundingVolume.End.Z),
                        }),
                        new OctreeNode(new AABB
                        {
                            Start = new Vector3(center.X, boundingVolume.Start.Y, center.Z),
                            End = new Vector3(boundingVolume.End.X, center.Y, boundingVolume.End.Z),
                        }),
                        new OctreeNode(new AABB
                        {
                            Start = new Vector3(center.X, center.Y, boundingVolume.Start.Z),
                            End = new Vector3(boundingVolume.End.X, boundingVolume.End.Y, center.Z),
                        }),
                        new OctreeNode(new AABB
                        {
                            Start = center,
                            End = boundingVolume.End,
                        })
                    };

                    // Redistribute the existing contents
                    var copy = units.ToArray();
                    units.Clear();
                    foreach (var existing in copy)
                    {
                        Insert(existing);
                    }
                }
                
                // If our AABB is contained wholly within any of our children, use that.
                foreach (var child in childNodes)
                {
                    if (Collision.IsContainedWithin(unit.BoundingBox, boundingVolume))
                    {
                        child.Insert(unit);
                        return;
                    }
                }

                // If it doesn't fit in any of our children, we gotta hold it here
                units.Add(unit);
            }
        }
    }
}
