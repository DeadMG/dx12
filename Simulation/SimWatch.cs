using System.Diagnostics;

namespace Simulation
{
    public class SimWatch
    {
        private readonly Stopwatch watch = new Stopwatch();
        private TimeSpan? markTime;

        public SimWatch()
        {
            watch.Start();
        }

        public TimeSpan MarkTime()
        {
            var time = watch.Elapsed;
            var delta = time - (markTime ?? TimeSpan.Zero);
            markTime = time;
            return delta;
        }
    }
}
