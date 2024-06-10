using Simulation;

namespace Platform.Contracts
{
    public class VolumeRenderTask
    {
        public required Camera Camera { get; set; }
        public required Volume Volume { get; set; }
    }
}
