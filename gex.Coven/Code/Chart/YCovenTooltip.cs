using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Drawing.Layouts;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Drawing.Layouts;
using LiveChartsCore.SkiaSharpView.SKCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Code.Chart {

    public class YCovenTooltip : SKDefaultTooltip {

        protected override void Initialize(LiveChartsCore.Chart chart) {
            base.Initialize(chart);
        }

        protected override Layout<SkiaSharpDrawingContext> GetLayout(IEnumerable<ChartPoint> foundPoints, LiveChartsCore.Chart chart) {

            StackLayout stack = new() {
                Orientation = ContainerOrientation.Vertical,
                HorizontalAlignment = Align.Start,
            };

            TableLayout table = new() {
                HorizontalAlignment = Align.Middle,
                VerticalAlignment = Align.Middle,
            };

            float maxWidth = (float)LiveCharts.DefaultSettings.MaxTooltipsAndLegendsLabelsWidth;

            List<string> labels = [];
            if (chart.View is ICartesianChartView cart) {
                labels = cart.XAxes.First()?.Labels?.ToList() ?? [];
            }

            int row = 0;

            IEnumerable<ChartPoint> sortedPoints = foundPoints.OrderByDescending(iter => iter.Coordinate.PrimaryValue);

            if (sortedPoints.Any()) {
                int x = (int)(sortedPoints.First()?.Coordinate.SecondaryValue ?? 0d);

                if (x >= 0 && x < labels.Count) {
                    string label = labels[x];

                    LabelGeometry xLabel = new() {
                        Text = $"{label}",
                        Paint = chart.GetTheme().TooltipTextPaint,
                        Padding = new(4, 4, 4, 8),
                        TextSize = 16,
                        MaxWidth = maxWidth,
                        VerticalAlign = Align.Start,
                        HorizontalAlign = Align.Start,
                    };

                    stack.Children.Add(xLabel);
                }
            }

            stack.Children.Add(table);

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

                if (y < 10) {
                    v = $"{y}";
                } else if (y < 1000) {
                    v = $"{Math.Round(y)}";
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
                // weird bug, setting Fill here makes the graph fill between the first and last points on the graph
                RectangleGeometry rect = new() {
                    Width = 12,
                    Height = 12,
                    Fill = mini.Stroke
                };

                table.AddChild(rect, row, 0);
                table.AddChild(label, row, 1);
                table.AddChild(value, row, 2);
                ++row;
            }

            return stack;
        }

    }
}
