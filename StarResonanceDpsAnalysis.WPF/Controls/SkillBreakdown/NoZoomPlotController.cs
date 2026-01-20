using OxyPlot;

namespace StarResonanceDpsAnalysis.WPF.Controls.SkillBreakdown;

public sealed class NoZoomPlotController : PlotController
{
    public NoZoomPlotController()
    {
        // Disable mouse-driven zooming while keeping other interactions (e.g., tracker, pan).
        Unbind(new OxyMouseDownGesture(OxyMouseButton.Middle));
        Unbind(new OxyMouseDownGesture(OxyMouseButton.Right, OxyModifierKeys.Control));
        Unbind(new OxyMouseDownGesture(OxyMouseButton.Left, OxyModifierKeys.Control | OxyModifierKeys.Alt));
        Unbind(new OxyMouseDownGesture(OxyMouseButton.XButton1));
        Unbind(new OxyMouseDownGesture(OxyMouseButton.XButton2));
        Unbind(new OxyMouseWheelGesture());
        Unbind(new OxyMouseWheelGesture(OxyModifierKeys.Control));

        // Disable right-click panning for the line chart.
        Unbind(new OxyMouseDownGesture(OxyMouseButton.Right));
    }
}
