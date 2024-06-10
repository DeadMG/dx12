using Data.Space;
using Platform.Contracts;
using Simulation;

namespace UI.Renderers
{
    public class HighlightRenderer : IDisposable
    {
        private readonly UnitHighlightRenderer highlightRenderer = new UnitHighlightRenderer();
        private IBrush? brush;

        public RGBA HighlightLineColour = new RGBA { B = 1, G = 1, R = 1, A = 1 };

        public void RenderHighlight(Camera camera, HashSet<Unit> highlight, IDraw draw)
        {
            if (highlight.Count == 0) return;

            brush = draw.GetOrCreateSolidBrush(brush, HighlightLineColour);

            foreach (var unit in highlight)
            {
                highlightRenderer.Render(unit, camera, brush, draw);
            }
        }

        public void Dispose()
        {
            brush?.Dispose();
        }
    }
}
