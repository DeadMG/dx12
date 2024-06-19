namespace Simulation
{
    public class Volume
    {
        private readonly HashSet<Unit> units = new HashSet<Unit>();

        public required Map Map { get; init; }

        public void Add(Unit unit) => units.Add(unit);
        public void Remove(Unit unit) =>  units.Remove(unit);

        public IEnumerable<Unit> Units => units;
    }
}
