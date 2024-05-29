using System.Diagnostics;

namespace Simulation
{
    public class Watch
    {
        private readonly Stopwatch watch = new Stopwatch();
        private TimeSpan? markTime;

        public Watch()
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
