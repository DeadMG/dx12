using Data;
using Renderer;
using SharpDX;
using Simulation;
using System.Collections.Concurrent;
using System.Drawing;
using System.Numerics;

namespace Application
{
    public class StandardControlScheme : IControlScheme
    {
        private readonly ConcurrentQueue<MouseWheelEvent> mouseWheelEvents = new ConcurrentQueue<MouseWheelEvent>();
        private readonly ConcurrentDictionary<Key, bool> keyState = new ConcurrentDictionary<Key, bool>();
        private readonly Latest<ScreenRectangle> selectionEvent = new Latest<ScreenRectangle>();
        private readonly Latest<ScreenPosition> rightMouseDownEvent = new Latest<ScreenPosition>();
        private readonly Latest<ScreenPosition> leftMouseDownEvent = new Latest<ScreenPosition>();
        private readonly Latest<ScreenPosition> mouseMoveEvent = new Latest<ScreenPosition>();
        private readonly Watch inputWatch = new Watch();
        private readonly Player player;
        private readonly Game game;

        private volatile ScreenSize viewSize;

        public StandardControlScheme(Player player, Game game, int width, int height)
        {
            this.player = player;
            this.game = game;
            viewSize = new ScreenSize { Height = height, Width = width };
        }

        public float ZoomCameraSensitivity { get; set; } = 1 / (float)10;
        public float PanCameraSensitivity { get; set; } = 1;

        public void Apply()
        {
            var size = viewSize; // Atomic read
            var world = player.ViewingWorld(game);
            var camera = player.CameraFor(world);
            var time = inputWatch.MarkTime();

            while (mouseWheelEvents.TryDequeue(out var wheelEvent))
            {
                var realLocation = Vector3.Transform(Vector3.Normalize(new Vector3(Space.Clip(wheelEvent.Position, size), wheelEvent.Amount)), camera.Orientation);

                // Scale the amount depending on the camera Y
                camera.Position += realLocation * camera.Position.Y * ZoomCameraSensitivity;
            }

            if (IsKeyDown(Key.W)) { Pan(camera, time, new Vector3(0, 1, 0)); }
            if (IsKeyDown(Key.S)) { Pan(camera, time, new Vector3(0, -1, 0)); }
            if (IsKeyDown(Key.A)) { Pan(camera, time, new Vector3(-1, 0, 0)); }
            if (IsKeyDown(Key.D)) { Pan(camera, time, new Vector3(1, 0, 0)); }

            Octree? octree = null;
            var mousePos = mouseMoveEvent.Read();
            if (mousePos != null)
            {
                octree = octree ?? world.CreateOctree();

                player.Hover = At(camera.Unproject(Space.Clip(mousePos, size), size), octree);
            }

            var leftDown = leftMouseDownEvent.Read();
            if (leftDown != null)
            {
                player.Selection.Clear();
            }

            if (leftDown != null && mousePos != null && !leftDown.Equals(mousePos))
            {
                octree = octree ?? world.CreateOctree();
                var frustum = camera.Unproject(Space.Clip(mousePos, size), Space.Clip(leftDown, size), size);
                player.Highlight.Clear();
                octree.Intersect(player.Highlight, frustum);
                player.SelectionBox = new ScreenRectangle { End = mousePos, Start = leftDown };
            }

            if (selectionEvent.Consume(out var selection))
            {
                octree = octree ?? world.CreateOctree();

                player.Highlight.Clear();
                player.SelectionBox = null;

                var frustum = camera.Unproject(Space.Clip(selection.Start, size), Space.Clip(selection.End, size), size);
                player.Selection.Clear();
                octree.Intersect(player.Selection, frustum);

                if (selection.End.Equals(selection.Start) && player.Selection.Count > 1)
                {
                    var closest = player.Selection.MinBy(x => (x.Position - camera.Position).Length()); 
                    player.Selection.Clear();
                    player.Selection.Add(closest);
                }
            }

            if (rightMouseDownEvent.Consume(out var rightMouseDown))
            {
                var location = camera.Unproject(Space.Clip(rightMouseDown, size), size);

                var order = new MoveOrder { Destination = location.AtY0() };
                foreach (var unit in player.Selection)
                {
                    if (!IsKeyDown(Key.Shift))
                    {
                        unit.Orders.Clear();
                    }
                    unit.Orders.Enqueue(order);
                }
            }
        }

        private void Pan(Camera camera, TimeSpan time, Vector3 vec)
        {
            camera.Position += Vector3.Transform(vec, camera.Orientation) * camera.Position.Y * PanCameraSensitivity * (float)time.TotalSeconds;
        }

        public void OnKeyDown(Key key)
        {
            keyState[key] = true;
        }

        public void OnKeyUp(Key key)
        {
            keyState[key] = false;
        }

        public void OnMouseWheel(float amount, ScreenPosition pos)
        {
            mouseWheelEvents.Enqueue(new MouseWheelEvent { Amount = amount, Position = pos });
        }

        public void OnResize(ScreenSize size)
        {
            viewSize = size;
        }

        private bool IsKeyDown(Key key)
        {
            if (keyState.TryGetValue(key, out var state)) return state;
            return false;
        }

        public void OnMouseDown(MouseButton button, ScreenPosition pos)
        {
            if (button == MouseButton.Left)
            {
                leftMouseDownEvent.Set(pos);
            }
            if (button == MouseButton.Right)
            {
                rightMouseDownEvent.Set(pos);
            }
        }

        public void OnMouseUp(MouseButton key, ScreenPosition pos)
        {
            if (key == MouseButton.Left)
            {
                if (leftMouseDownEvent.Consume(out var down))
                {
                    selectionEvent.Set(new ScreenRectangle { Start = down, End = pos });
                }
            }
        }

        public void OnMouseMove(ScreenPosition pos)
        {
            mouseMoveEvent.Set(pos);
        }

        private Unit? At(Ray ray, Octree octree)
        {
            var world = player.ViewingWorld(game);
            var camera = player.CameraFor(world);
            var units = octree.Intersect(ray);

            if (units.Count == 0) return null;
            if (units.Count == 1) return units.First();

            return units.OrderBy(x => Vector3.Distance(x.Position, camera.Position)).First();
        }

        private class MouseWheelEvent
        {
            public required float Amount { get; init; }
            public required ScreenPosition Position { get; init; }
        }
    }
}
