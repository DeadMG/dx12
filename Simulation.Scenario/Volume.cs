﻿using System.Numerics;

namespace Simulation
{
    public class Volume
    {
        private readonly HashSet<Unit> units = new HashSet<Unit>();
        private readonly Vector3 dimensions; // A box centered on 0,0,0

        public Volume(Vector3 dimensions)
        {
            this.dimensions = dimensions;
        }

        public void Add(Unit unit) => units.Add(unit);
        public void Remove(Unit unit) =>  units.Remove(unit);
        public Vector3 Dimensions => dimensions;
        public IEnumerable<Unit> Units => units;
    }
}