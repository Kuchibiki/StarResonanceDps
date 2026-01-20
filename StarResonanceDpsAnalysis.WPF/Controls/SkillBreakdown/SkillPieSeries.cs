using System;
using System.Collections.Generic;
using System.Globalization;
using OxyPlot;
using OxyPlot.Series;

namespace StarResonanceDpsAnalysis.WPF.Controls.SkillBreakdown;

public sealed class SkillPieSeries : PieSeries
{
    private readonly List<IList<ScreenPoint>> _slicePoints = new();
    private double _total;

    public sealed record SliceInfo(string SkillName, long SkillId, long Value, string HumanReadableValue);

    public IDictionary<PieSlice, SliceInfo> SliceInfoMap { get; } = new Dictionary<PieSlice, SliceInfo>();
    public ISet<PieSlice> HideInsideLabelSlices { get; } = new HashSet<PieSlice>();
    public ISet<PieSlice> HideOutsideLabelSlices { get; } = new HashSet<PieSlice>();

    public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
    {
        for (var i = 0; i < _slicePoints.Count; i++)
        {
            if (!ScreenPointHelper.IsPointInPolygon(point, _slicePoints[i]))
            {
                continue;
            }

            var slice = Slices[i];
            var item = GetItem(i);
            var percent = _total <= 0 ? 0 : slice.Value / _total * 100;

            if (SliceInfoMap.TryGetValue(slice, out var info))
            {
                return new TrackerHitResult
                {
                    Series = this,
                    Position = point,
                    Item = item,
                    Index = i,
                    Text = StringHelper.Format(
                        ActualCulture,
                        TrackerFormatString,
                        slice,
                        Title,
                        info.SkillName,
                        slice.Value,
                        percent,
                        info.SkillId,
                        info.HumanReadableValue)
                };
            }

            return new TrackerHitResult
            {
                Series = this,
                Position = point,
                Item = item,
                Index = i,
                Text = StringHelper.Format(
                    ActualCulture,
                    TrackerFormatString,
                    slice,
                    Title,
                    slice.Label,
                    slice.Value,
                    percent,
                    0,
                    string.Empty)
            };
        }

        return null;
    }

    public override void Render(IRenderContext rc)
    {
        _slicePoints.Clear();

        if (Slices.Count == 0)
        {
            return;
        }

        _total = 0;
        foreach (var slice in Slices)
        {
            _total += slice.Value;
        }

        if (Math.Abs(_total) <= 0)
        {
            return;
        }

        // todo: reduce available size due to the labels
        var radius = Math.Min(PlotModel.PlotArea.Width, PlotModel.PlotArea.Height) / 2;

        var outerRadius = radius * (Diameter - ExplodedDistance);
        var innerRadius = radius * InnerDiameter;

        var angle = StartAngle;
        var midPoint = new ScreenPoint(
            (PlotModel.PlotArea.Left + PlotModel.PlotArea.Right) * 0.5,
            (PlotModel.PlotArea.Top + PlotModel.PlotArea.Bottom) * 0.5);

        foreach (var slice in Slices)
        {
            var outerPoints = new List<ScreenPoint>();
            var innerPoints = new List<ScreenPoint>();

            var sliceAngle = slice.Value / _total * AngleSpan;
            var endAngle = angle + sliceAngle;
            var explodedRadius = slice.IsExploded ? ExplodedDistance * radius : 0.0;

            var midAngle = angle + (sliceAngle / 2);
            var midAngleRadians = midAngle * Math.PI / 180;
            var mp = new ScreenPoint(
                midPoint.X + (explodedRadius * Math.Cos(midAngleRadians)),
                midPoint.Y + (explodedRadius * Math.Sin(midAngleRadians)));

            // Create the pie sector points for both outside and inside arcs
            while (true)
            {
                var stop = false;
                if (angle >= endAngle)
                {
                    angle = endAngle;
                    stop = true;
                }

                var a = angle * Math.PI / 180;
                var op = new ScreenPoint(mp.X + (outerRadius * Math.Cos(a)), mp.Y + (outerRadius * Math.Sin(a)));
                outerPoints.Add(op);
                var ip = new ScreenPoint(mp.X + (innerRadius * Math.Cos(a)), mp.Y + (innerRadius * Math.Sin(a)));
                if (innerRadius + explodedRadius > 0)
                {
                    innerPoints.Add(ip);
                }

                if (stop)
                {
                    break;
                }

                angle += AngleIncrement;
            }

            innerPoints.Reverse();
            if (innerPoints.Count == 0)
            {
                innerPoints.Add(mp);
            }

            innerPoints.Add(outerPoints[0]);

            var points = outerPoints;
            points.AddRange(innerPoints);

            rc.DrawPolygon(points, slice.ActualFillColor, Stroke, StrokeThickness, EdgeRenderingMode, null, LineJoin.Bevel);

            // keep the point for hit testing
            _slicePoints.Add(points);

            // Render label outside the slice
            if (OutsideLabelFormat != null && !HideOutsideLabelSlices.Contains(slice))
            {
                var label = string.Format(
                    CultureInfo.CurrentUICulture,
                    OutsideLabelFormat,
                    slice.Value,
                    slice.Label,
                    slice.Value / _total * 100);
                var sign = Math.Sign(Math.Cos(midAngleRadians));

                // tick points
                var tp0 = new ScreenPoint(
                    mp.X + ((outerRadius + TickDistance) * Math.Cos(midAngleRadians)),
                    mp.Y + ((outerRadius + TickDistance) * Math.Sin(midAngleRadians)));
                var tp1 = new ScreenPoint(
                    tp0.X + (TickRadialLength * Math.Cos(midAngleRadians)),
                    tp0.Y + (TickRadialLength * Math.Sin(midAngleRadians)));
                var tp2 = new ScreenPoint(tp1.X + (TickHorizontalLength * sign), tp1.Y);

                // draw the tick line with the same color as the text
                rc.DrawLine(new[] { tp0, tp1, tp2 }, ActualTextColor, 1, EdgeRenderingMode, null, LineJoin.Bevel);

                // label
                var labelPosition = new ScreenPoint(tp2.X + (TickLabelDistance * sign), tp2.Y);
                rc.DrawText(
                    labelPosition,
                    label,
                    ActualTextColor,
                    ActualFont,
                    ActualFontSize,
                    ActualFontWeight,
                    0,
                    sign > 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                    VerticalAlignment.Middle);
            }

            // Render a label inside the slice
            if (InsideLabelFormat != null && !HideInsideLabelSlices.Contains(slice) && !InsideLabelColor.IsUndefined())
            {
                var label = string.Format(
                    CultureInfo.CurrentUICulture,
                    InsideLabelFormat,
                    slice.Value,
                    slice.Label,
                    slice.Value / _total * 100);
                var r = (innerRadius * (1 - InsideLabelPosition)) + (outerRadius * InsideLabelPosition);
                var labelPosition = new ScreenPoint(
                    mp.X + (r * Math.Cos(midAngleRadians)), mp.Y + (r * Math.Sin(midAngleRadians)));
                var textAngle = 0.0;
                if (AreInsideLabelsAngled)
                {
                    textAngle = midAngle;
                    if (Math.Cos(midAngleRadians) < 0)
                    {
                        textAngle += 180;
                    }
                }

                var actualInsideLabelColor = InsideLabelColor.IsAutomatic() ? ActualTextColor : InsideLabelColor;

                rc.DrawText(
                    labelPosition,
                    label,
                    actualInsideLabelColor,
                    ActualFont,
                    ActualFontSize,
                    ActualFontWeight,
                    textAngle,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Middle);
            }
        }
    }
}
