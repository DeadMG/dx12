using Simulation;
using Xunit;

namespace SimulationTests
{
    public class SimRateTests
    {
        [Fact]
        public void NoTicksForNoTime()
        {
            var rate = new SimRate();
            Assert.Equal(0u, rate.TicksFor(TimeSpan.Zero));
        }

        [Fact]
        public void OneTicksForSomeTime()
        {
            var rate = new SimRate();
            Assert.Equal(1u, rate.TicksFor(rate.RealTimePerTick));
        }

        [Fact]
        public void CarriesOverPartialTicks()
        {
            var rate = new SimRate();
            Assert.Equal(1u, rate.TicksFor(rate.RealTimePerTick * 1.5f));
            Assert.Equal(2u, rate.TicksFor(rate.RealTimePerTick * 1.5f));
        }

        [Fact]
        public void HandlesChangedTickRate()
        {
            var rate = new SimRate();
            var baseTime = rate.RealTimePerTick;
            Assert.Equal(1u, rate.TicksFor(baseTime));
            rate.TicksPerSecond = rate.TicksPerSecond * 2;
            Assert.Equal(2u, rate.TicksFor(baseTime));
        }

        [Fact]
        public void HandlesPartialTicksWithChangedTickRate()
        {
            var rate = new SimRate();
            var baseTime = rate.RealTimePerTick;
            Assert.Equal(1u, rate.TicksFor(baseTime * 1.5));
            rate.TicksPerSecond = rate.TicksPerSecond * 2;
            Assert.Equal(1u, rate.TicksFor(baseTime * 0.26)); // FP inaccuracy; add a little extra to make sure we go over
        }

        [Fact]
        public void ChangingTicksChangesRealTimePerTick()
        {
            var rate = new SimRate();
            var baseTime = rate.RealTimePerTick;
            rate.TicksPerSecond = rate.TicksPerSecond * 2;
            Assert.Equal(2, baseTime / rate.RealTimePerTick);
        }

        [Fact]
        public void ChangingTicksDoesntChangeStepSize()
        {
            var rate = new SimRate();
            var stepSize = rate.TickStep;
            rate.TicksPerSecond = rate.TicksPerSecond * 2;
            Assert.Equal(stepSize, rate.TickStep);
        }
    }
}
