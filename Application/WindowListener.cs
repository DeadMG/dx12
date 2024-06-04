using Data.Space;
using Platform.Contracts;
using Util;

namespace Application
{
    public class WindowListener : IWindowListener
    {
        private readonly IControlScheme scheme;

        public WindowListener(IControlScheme scheme)
        {
            this.scheme = scheme;
        }

        public void OnKeyDown(Key key) => scheme.OnKeyDown(key);
        public void OnKeyUp(Key key) => scheme.OnKeyUp(key);
        public void OnMouseDown(MouseButton key, ScreenPosition pos) => scheme.OnMouseDown(key, pos);
        public void OnMouseUp(MouseButton key, ScreenPosition pos) => scheme.OnMouseUp(key, pos);
        public void OnMouseWheel(float amount, ScreenPosition pos) => scheme.OnMouseWheel(amount, pos);
        public void OnMouseMove(ScreenPosition pos) => scheme.OnMouseMove(pos);

        public void OnResize(ScreenSize size) => Resize.Set(size);

        public readonly LatestValue<ScreenSize> Resize = new LatestValue<ScreenSize>();
    }
}
