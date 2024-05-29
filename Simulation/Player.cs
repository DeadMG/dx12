using Data;

namespace Simulation
{
    public class Player
    {
        public required Force Force { get; init; }
        public Unit? Hover { get; set; }
        public HashSet<Unit> Highlight { get; } = new HashSet<Unit>();
        public HashSet<Unit> Selection { get; } = new HashSet<Unit>();
        public Dictionary<World, Camera> Cameras { get; } = new Dictionary<World, Camera>();
        public World? CurrentWorld { get; set; }
        public ScreenRectangle? SelectionHighlight { get; set; }

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
