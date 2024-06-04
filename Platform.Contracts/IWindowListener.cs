using Data.Space;

namespace Platform.Contracts
{
    public interface IWindowListener
    {
        public void OnKeyDown(Key key);
        public void OnKeyUp(Key key);
        public void OnMouseDown(MouseButton button, ScreenPosition pos);
        public void OnMouseUp(MouseButton button, ScreenPosition pos);
        public void OnMouseWheel(float amount, ScreenPosition pos);
        public void OnMouseMove(ScreenPosition pos);

        public void OnResize(ScreenSize size);
    }
}
