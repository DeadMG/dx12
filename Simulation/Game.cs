using System.Numerics;

namespace Simulation
{
    public class Game
    {
        public readonly List<Force> Forces = new List<Force>();
        public readonly List<World> Worlds = new List<World>();
        public readonly List<Player> Players = new List<Player>();
        public readonly SimRate Rate = new SimRate();

        public async Task Update(TimeSpan realElapsedTime)
        {
            var ticks = Rate.TicksFor(realElapsedTime);
            for (int i = 0; i < ticks; i++)
            {
                Tick();
            }
        }

        public World AddWorld()
        {
            var world = new World();
            Worlds.Add(world);
            return world;
        }

        public Force AddForce()
        {
            var force = new Force();
            Forces.Add(force);
            return force;
        }

        public Player AddPlayer(Force force)
        {
            var player = new Player { Force = force };
            Players.Add(player);
            return player;
        }

        private void Tick()
        {
            var totalSimTime = Rate.Ticks * Rate.TickStep;

            foreach (var world in Worlds)
            {
                foreach (var unit in world.Units)
                {
                    unit.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)totalSimTime.TotalSeconds * (float)Math.PI);
                }
            }
        }
    }
}
