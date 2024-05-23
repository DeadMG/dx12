namespace Simulation
{
    public class SimRate
    {
        private double partialTick = 0;
        private uint ticks = 0;

        public uint TicksFor(TimeSpan realElapsedTime)
        {
            var totalExtra = partialTick + (realElapsedTime / RealTimePerTick);
            var newTicks = (uint)Math.Floor(totalExtra);

            ticks += newTicks;
            partialTick = totalExtra - newTicks;

            return newTicks;
        }

        public uint TicksPerSecond = 60;

        public uint Ticks => ticks;
        public TimeSpan RealTimePerTick => TimeSpan.FromSeconds(1 / (double)TicksPerSecond);
        public TimeSpan TickStep => TimeSpan.FromSeconds(1 / (double)60);
    }
}
