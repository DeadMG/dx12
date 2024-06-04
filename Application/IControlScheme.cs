using Data.Space;
using Platform.Contracts;

namespace Application
{
    public interface IControlScheme
    {
        public void OnMouseWheel(float amount, ScreenPosition pos);
        public void OnKeyDown(Key key);
        public void OnKeyUp(Key key);
        public void OnMouseDown(MouseButton key, ScreenPosition pos);
        public void OnMouseUp(MouseButton key, ScreenPosition pos);
        public void OnMouseMove(ScreenPosition pos);

        public void Apply();
    }
}
