using Data.Space;
using Simulation;
using System.Numerics;

namespace UI
{
    public class Scenario
    {
        private ScreenSize screenSize;
        private Volume currentVolume;

        public Scenario(ScreenSize screenSize, Volume currentVolume)
        {
            this.screenSize = screenSize;
            this.currentVolume = currentVolume;
        }

        public Unit? Hover { get; set; }
        public HashSet<Unit> Highlight { get; } = new HashSet<Unit>();
        public HashSet<Unit> Selection { get; } = new HashSet<Unit>();
        public Dictionary<Volume, Camera> Cameras { get; } = new Dictionary<Volume, Camera>();
        public ScreenRectangle? SelectionBox { get; set; }

        public Camera CameraFor(Volume volume)
        {
            if (!Cameras.ContainsKey(volume)) Cameras[volume] = new Camera(screenSize, new Vector3(0, 30, 0), Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), 90f.ToRadians()), 90f);
            return Cameras[volume];
        }

        public Volume CurrentVolume
        {
            get => currentVolume;
            set => currentVolume = value;
        }

        public ScreenSize ScreenSize
        {
            get => screenSize;
            set
            {
                screenSize = value;
                foreach (var camera in Cameras)
                {
                    camera.Value.Resize(screenSize);
                }
            }
        }

        public Camera CurrentCamera => CameraFor(currentVolume);
    }
}
