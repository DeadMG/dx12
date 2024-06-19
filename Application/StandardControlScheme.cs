using Data.Space;
using Platform.Contracts;
using Simulation;
using Simulation.Physics;
using System.Collections.Concurrent;
using System.Numerics;
using Util;

namespace Application
{
    public class StandardControlScheme : IControlScheme
    {
        private readonly ConcurrentQueue<MouseWheelEvent> mouseWheelEvents = new ConcurrentQueue<MouseWheelEvent>();
        private readonly ConcurrentDictionary<Key, bool> keyState = new ConcurrentDictionary<Key, bool>();
        private readonly LatestValue<ScreenRectangle> selectionEvent = new LatestValue<ScreenRectangle>();
        private readonly LatestValue<ScreenPosition> rightMouseDownEvent = new LatestValue<ScreenPosition>();
        private readonly LatestValue<ScreenPosition> leftMouseDownEvent = new LatestValue<ScreenPosition>();
        private readonly LatestValue<ScreenPosition> mouseMoveEvent = new LatestValue<ScreenPosition>();
        private readonly Watch inputWatch = new Watch();

        private readonly UI.Scenario uiState;

        public StandardControlScheme(UI.Scenario uiState)
        {
            this.uiState = uiState;
        }

        public float ZoomCameraSensitivity { get; set; } = 1 / (float)10;
        public float PanCameraSensitivity { get; set; } = 1;

        public void Apply()
        {
            var camera = uiState.CurrentCamera;
            var time = inputWatch.MarkTime();

            while (mouseWheelEvents.TryDequeue(out var wheelEvent))
            {
                var realLocation = Vector3.Transform(Vector3.Normalize(new Vector3(Project.Clip(wheelEvent.Position, uiState.ScreenSize), wheelEvent.Amount)), camera.Orientation);

                // Scale the amount depending on the camera Y
                camera.Position += realLocation * camera.Position.Y * ZoomCameraSensitivity;
            }

            if (IsKeyDown(Key.W)) { Pan(camera, time, new Vector3(0, 1, 0)); }
            if (IsKeyDown(Key.S)) { Pan(camera, time, new Vector3(0, -1, 0)); }
            if (IsKeyDown(Key.A)) { Pan(camera, time, new Vector3(-1, 0, 0)); }
            if (IsKeyDown(Key.D)) { Pan(camera, time, new Vector3(1, 0, 0)); }

            Octree<Unit>? octree = null;
            var mousePos = mouseMoveEvent.Read();
            if (mousePos != null)
            {
                octree = octree ?? new Octree<Unit>(uiState.CurrentVolume.Units, uiState.CurrentVolume.Map.Dimensions);

                uiState.Hover = At(uiState.CurrentCamera, Ray.FromScreen(mousePos.Value, uiState.ScreenSize, uiState.CurrentCamera.InvViewProjection), octree);
            }

            var leftDown = leftMouseDownEvent.Read();
            if (leftDown != null)
            {
                uiState.Selection.Clear();
            }

            if (leftDown != null && mousePos != null && !leftDown.Equals(mousePos))
            {
                octree = octree ?? new Octree<Unit>(uiState.CurrentVolume.Units, uiState.CurrentVolume.Map.Dimensions);

                var frustum = Frustum.FromScreen(ScreenRectangle.FromPoints(mousePos.Value, leftDown.Value), uiState.ScreenSize, uiState.CurrentCamera.InvViewProjection);
                uiState.Highlight.Clear();
                octree.Intersect(uiState.Highlight, frustum);
                uiState.SelectionBox = new ScreenRectangle { End = mousePos.Value, Start = leftDown.Value };
            }

            if (selectionEvent.TryConsume(out var selection))
            {
                octree = octree ?? new Octree<Unit>(uiState.CurrentVolume.Units, uiState.CurrentVolume.Map.Dimensions);

                uiState.Highlight.Clear();
                uiState.SelectionBox = null;

                var frustum = Frustum.FromScreen(selection.Value, uiState.ScreenSize, uiState.CurrentCamera.InvViewProjection);
                uiState.Selection.Clear();
                octree.Intersect(uiState.Selection, frustum);

                if (selection.Value.End.Equals(selection.Value.Start) && uiState.Selection.Count > 1)
                {
                    var closest = uiState.Selection.MinBy(x => (x.Position - camera.Position).Length());
                    uiState.Selection.Clear();
                    uiState.Selection.Add(closest);
                }
            }

            if (rightMouseDownEvent.TryConsume(out var rightMouseDown))
            {
                var location = Ray.FromScreen(rightMouseDown.Value, uiState.ScreenSize, uiState.CurrentCamera.InvViewProjection);

                var order = new MoveOrder { Destination = location.AtY0() };
                foreach (var unit in uiState.Selection)
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
                if (leftMouseDownEvent.TryConsume(out var down))
                {
                    selectionEvent.Set(new ScreenRectangle { Start = down.Value, End = pos });
                }
            }
        }

        public void OnMouseMove(ScreenPosition pos)
        {
            mouseMoveEvent.Set(pos);
        }

        private Unit? At(Camera camera, Ray ray, Octree<Unit> octree)
        {
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
