using Data;
using System.Diagnostics;
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

            for (uint i = 0; i < ticks; ++i)
            {
                Tick();
            }
        }

        public World AddWorld(Vector3 dimensions)
        {
            var world = new World(dimensions);
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
            foreach (var world in Worlds)
            {
                foreach (var unit in world.Units)
                {
                    if (unit.Orders.TryPeek(out var order))
                    {
                        if (order is MoveOrder move)
                        {
                            // First, try to turn towards the target
                            var distance = move.Destination - unit.Position;
                            var targetVector = Vector3.Normalize(distance);
                            var currentHeading = Vector3.Transform(Mesh.Facing, unit.Orientation);

                            var axis = Vector3.Normalize(Vector3.Cross(-targetVector, currentHeading));
                            var angle = (float)Math.Acos(Vector3.Dot(targetVector, currentHeading));

                            if (!float.IsNaN(axis.X) && !float.IsNaN(angle))
                            {
                                if (angle > 1) angle = 1;
                                if (angle < -1) angle = -1;

                                var maxTurnRate = unit.Blueprint.TurnRate * Rate.TickStep.TotalSeconds;
                                angle = angle > 0 ? (float)Math.Min(angle, maxTurnRate) : (float)Math.Max(angle, -maxTurnRate);

                                unit.Orientation = Quaternion.Normalize(unit.Orientation * Quaternion.CreateFromAxisAngle(axis, angle));
                                Debug.Assert(!float.IsNaN(unit.Orientation.X));
                                currentHeading = Vector3.Transform(Mesh.Facing, unit.Orientation);
                            }

                            var maxAcceleration = unit.Blueprint.Acceleration * Rate.TickStep.TotalSeconds;
                            var testPosition = unit.Position + (currentHeading * unit.Velocity * (unit.Velocity / unit.Blueprint.Acceleration));
                            var testDistance = testPosition - unit.Position;

                            if (testDistance.Length() > distance.Length() || distance.Length() < 0.1f)
                            {
                                // Need to slow down so that we can hit the target
                                unit.Velocity = (float)Math.Max(unit.Velocity - maxAcceleration, 0);
                            }
                            else
                            {
                                unit.Velocity = (float)Math.Min(unit.Velocity + maxAcceleration, unit.Blueprint.MaxSpeed);
                            }

                            if (unit.Velocity == 0)
                            {
                                // We have reached our destination and should just stop here, yep.
                                unit.Orders.Dequeue();
                            } 
                            else
                            {
                                unit.Position += currentHeading * (unit.Velocity * (float)Rate.TickStep.TotalSeconds);
                            }
                        }
                    }
                }
            }
        }
    }
}
