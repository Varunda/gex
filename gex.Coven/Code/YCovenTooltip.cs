using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Drawing.Layouts;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Drawing.Layouts;
using LiveChartsCore.SkiaSharpView.SKCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Code {

    public class YCovenTooltip : SKDefaultTooltip {

        protected override void Initialize(Chart chart) {
            base.Initialize(chart);
        }

        protected override Layout<SkiaSharpDrawingContext> GetLayout(IEnumerable<ChartPoint> foundPoints, Chart chart) {

            TableLayout table = new() {
                HorizontalAlignment = Align.Middle,
                VerticalAlignment = Align.Middle,
            };

            float maxWidth = (float)LiveCharts.DefaultSettings.MaxTooltipsAndLegendsLabelsWidth;

            IEnumerable<ChartPoint> sortedPoints = foundPoints.OrderByDescending(iter => iter.Coordinate.PrimaryValue);

            for (int i = 0; i < sortedPoints.Count(); ++i) {
                ChartPoint point = sortedPoints.ElementAt(i);

                LabelGeometry label = new() {
                    Text = $"{point.Context.Series.Name}",
                    Paint = chart.GetTheme().TooltipTextPaint,
                    Padding = new(8, 0),
                    TextSize = 16,
                    MaxWidth = maxWidth,
                    VerticalAlign = Align.Start,
                    HorizontalAlign = Align.Start,
                };

                double y = point.Coordinate.PrimaryValue;
                string v = $"{y}";

                if (y < 1000) {
                    v = $"{y}";
                } else if (y < 1_000_000) {
                    v = $"{Math.Round(y / 1000d, 2)}K";
                } else {
                    v = $"{Math.Round(y / 1_000_000d, 2)}m";
                }

                LabelGeometry value = new() {
                    Text = v,
                    Paint = chart.GetTheme().TooltipTextPaint,
                    Padding = new(8, 0),
                    TextSize = 16,
                    MaxWidth = maxWidth,
                    VerticalAlign = Align.Start,
                    HorizontalAlign = Align.Start,
                };

                IDrawnElement<SkiaSharpDrawingContext> mini = (IDrawnElement<SkiaSharpDrawingContext>)point.Context.Series.GetMiniatureGeometry(point);
                table.AddChild(mini, i, 0);
                table.AddChild(label, i, 1);
                table.AddChild(value, i, 2);
            }

            return table;
        }

    }
}
