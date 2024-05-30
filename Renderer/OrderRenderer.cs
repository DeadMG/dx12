using Data;
using Simulation;
using System.Numerics;
using Wrapper.Direct2D;

namespace Renderer
{
    internal class OrderRenderer
    {
        public Colour MoveOrderColour { get; set; } = new Colour { B = 0.8f, G = 0.7f, R = 0, A = 0.5f };

        internal void Render(RendererParameters rp, DrawContext draw)
        {
            var orderData = new Dictionary<IOrder, OrderData>();

            foreach (var unit in rp.World.Units)
            {
                IOrder? orderOrigin = null;

                foreach (var order in unit.Orders)
                {
                    if (!orderData.ContainsKey(order)) orderData[order] = new OrderData();

                    var data = orderData[order];    
                    if (orderOrigin != null)
                    {
                        data.Origins.Add(orderOrigin);
                    } 
                    else
                    {
                        data.Units.Add(unit);
                    }

                    orderOrigin = order.Destination.HasValue ? order : orderOrigin;
                }
            }

            var moveBrush = rp.Tracker.Track(draw.CreateSolidBrush(MoveOrderColour));
            foreach (var data in orderData)
            {
                if (data.Value.Units.Count > 0)
                {
                    var averageOrigin = data.Value.Units.Aggregate(new Vector3(), (x, y) => x + y.Position) / data.Value.Units.Count;

                    if (data.Key is MoveOrder move)
                    {
                        var screenStart = Space.Screen(averageOrigin, rp.VPMatrix, rp.ScreenSize);
                        var screenEnd = Space.Screen(move.Destination, rp.VPMatrix, rp.ScreenSize);

                        draw.DrawLine(screenStart, screenEnd, moveBrush, ScaleStrokeWidth(100, rp.Player, rp.World));
                    }
                }

                foreach (var orderOrigin in data.Value.Origins)
                {
                    if (data.Key is MoveOrder move)
                    {
                        var screenStart = Space.Screen(orderOrigin.Destination.Value, rp.VPMatrix, rp.ScreenSize);
                        var screenEnd = Space.Screen(move.Destination, rp.VPMatrix, rp.ScreenSize);

                        draw.DrawLine(screenStart, screenEnd, moveBrush, ScaleStrokeWidth(100, rp.Player, rp.World));
                    }
                }
            }
        }

        private float ScaleStrokeWidth(float stroke, Player player, World world)
        {
            return (float)Math.Ceiling((stroke * 30) / player.CameraFor(world).Position.Y);
        }

        private class OrderData
        {
            public HashSet<IOrder> Origins { get; } = new HashSet<IOrder>();
            public HashSet<Unit> Units { get; } = new HashSet<Unit>();
        }
    }
}
