using System.Diagnostics;

namespace Data.Space
{
    public class Watch
    {
        private readonly Stopwatch watch = new Stopwatch();

        public Watch()
        {
            watch.Start();
        }

        public TimeSpan MarkTime()
        {
            var time = watch.Elapsed;
            watch.Restart();
            return time;
        }
    }
}
