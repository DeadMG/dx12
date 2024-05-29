using System.Numerics;

namespace Simulation
{
    public class MoveOrder : IOrder
    {
        public required Vector3 Destination { get; init; }

        Vector3? IOrder.Destination => Destination;
    }
}
