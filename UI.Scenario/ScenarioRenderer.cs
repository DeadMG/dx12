using Platform.Contracts;
using UI.Renderers;

namespace UI
{
    public class ScenarioRenderer : IDisposable
    {
        public HighlightRenderer HighlightRenderer { get; } = new HighlightRenderer();
        public HoverRenderer HoverRenderer { get; } = new HoverRenderer();
        public OrderRenderer OrderRenderer { get; } = new OrderRenderer();
        public SelectionBoxRenderer SelectionBoxRenderer { get; } = new SelectionBoxRenderer();
        public SelectionRenderer SelectionRenderer { get; } = new SelectionRenderer();

        public void Render(Scenario scenario, IDraw draw)
        {
            HighlightRenderer.RenderHighlight(scenario.CurrentCamera, scenario.Highlight, draw);
            HoverRenderer.RenderHover(scenario.CurrentCamera, scenario.Hover, draw);
            OrderRenderer.Render(scenario.CurrentCamera, scenario.CurrentVolume, draw);
            if (scenario.SelectionBox != null) SelectionBoxRenderer.RenderSelectionBox(scenario.SelectionBox.Value, draw);
            SelectionRenderer.RenderSelection(scenario.CurrentCamera, scenario.Selection, draw);
        }

        public void Dispose()
        {
            HighlightRenderer.Dispose();
            HoverRenderer.Dispose();
            OrderRenderer.Dispose();
            SelectionBoxRenderer.Dispose();
            SelectionRenderer.Dispose();
        }
    }
}
