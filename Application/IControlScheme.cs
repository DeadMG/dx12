using Simulation;

namespace Application
{
    public interface IControlScheme
    {
        public void OnMouseWheel(float amount, int x, int y);

        public void OnResize(int width, int height);

        public void Apply(Game g);
    }
}
