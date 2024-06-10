using Data.Space;
using Platform.Contracts;
using Simulation;
using System.Numerics;

namespace UI.Renderers
{
    public class OrderRenderer : IDisposable
    {
        private IBrush? brush;

        public RGBA MoveOrderColour { get; set; } = new RGBA { B = 0.8f, G = 0.7f, R = 0, A = 0.5f };

        public void Render(Camera camera, Volume volume, IDraw draw)
        {
            var orderData = new Dictionary<IOrder, OrderData>();

            brush = draw.GetOrCreateSolidBrush(brush, MoveOrderColour);

            foreach (var unit in volume.Units)
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

                    orderOrigin = order;
                }
            }

            foreach (var data in orderData)
            {
                if (data.Value.Units.Count > 0)
                {
                    var averageOrigin = data.Value.Units.Aggregate(new Vector3(), (x, y) => x + y.Position) / data.Value.Units.Count;

                    if (data.Key is MoveOrder move)
                    {
                        var screenStart = Project.Screen(averageOrigin, camera.ViewProjection, camera.ScreenSize);
                        var screenEnd = Project.Screen(move.Destination, camera.ViewProjection, camera.ScreenSize);

                        var distance = (camera.Position - averageOrigin).Length();

                        draw.DrawLine(new ScreenLine(screenStart, screenEnd), brush, 600 / distance);
                    }
                }

                foreach (var orderOrigin in data.Value.Origins)
                {
                    if (data.Key is MoveOrder move)
                    {
                        var screenStart = Project.Screen(orderOrigin.Destination, camera.ViewProjection, camera.ScreenSize);
                        var screenEnd = Project.Screen(move.Destination, camera.ViewProjection, camera.ScreenSize);

                        var distance = (camera.Position - orderOrigin.Destination).Length();

                        draw.DrawLine(new ScreenLine(screenStart, screenEnd), brush, 600 / distance);
                    }
                }
            }
        }

        public void Dispose()
        {
            brush?.Dispose();
        }

        private class OrderData
        {
            public HashSet<IOrder> Origins { get; } = new HashSet<IOrder>();
            public HashSet<Unit> Units { get; } = new HashSet<Unit>();
        }
    }
}
