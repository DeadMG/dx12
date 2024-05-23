using System.Numerics;

namespace Simulation
{
    public class Player
    {
        public required Force Force { get; init; }
        public Dictionary<World, Camera> Cameras { get; } = new Dictionary<World, Camera>();
        public World? CurrentWorld { get; set; }

        public World ViewingWorld(Game game)
        {
            return CurrentWorld ?? game.Worlds.First();
        }

        public Camera CameraFor(World world)
        {
            if (!Cameras.ContainsKey(world)) Cameras[world] = new Camera();
            return Cameras[world];
        }
    }
}
