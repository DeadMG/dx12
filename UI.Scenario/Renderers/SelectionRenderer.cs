using Data.Space;
using Simulation;
using Platform.Contracts;

namespace UI.Renderers
{
    public class SelectionRenderer : IDisposable
    {
        private readonly UnitHighlightRenderer highlightRenderer = new UnitHighlightRenderer();

        private IBrush? brush;

        public RGBA SelectionLineColour = new RGBA { B = 1, G = 1, R = 1, A = 1 };

        public void RenderSelection(Camera camera, HashSet<Unit> selection, IDraw draw)
        {
            if (selection.Count == 0) return;

            brush = draw.GetOrCreateSolidBrush(brush, SelectionLineColour);

            foreach (var unit in selection)
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
