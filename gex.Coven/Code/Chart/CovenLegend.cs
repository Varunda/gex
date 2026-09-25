using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Drawing.Layouts;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Drawing.Layouts;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Code.Chart {

    public class CovenLegend : SKDefaultLegend {

        protected override Layout<SkiaSharpDrawingContext> GetLayout(LiveChartsCore.Chart chart) {
            Theme theme = chart.GetTheme();

            StackLayout stack = new() {
                Orientation = ContainerOrientation.Vertical,
                Padding = new Padding(4, 4),
                HorizontalAlignment = Align.Start,
                VerticalAlignment = Align.Middle,
            };

            foreach (ISeries series in chart.Series.Where(iter => iter.IsVisibleAtLegend == true)) {
                stack.Children.Add(new LegendItem(series.Name ?? "<who?>", series, theme.LegendTextPaint));
            }

            return stack;
        }

    }

    public class LegendItem : StackLayout {

        public string Name { get; private set; } = "";

        public LegendItem(string label, ISeries series, Paint? textPaint) {
            Name = label;

            Orientation = ContainerOrientation.Horizontal;
            Padding = new Padding(8, 4);
            VerticalAlignment = Align.Middle;
            HorizontalAlignment = Align.Middle;
            Opacity = series.IsVisible ? 1 : 0.5f;

            IDrawnElement<SkiaSharpDrawingContext> miniature = (IDrawnElement<SkiaSharpDrawingContext>)series.GetMiniatureGeometry(null);
            if (miniature is BoundedDrawnGeometry bounded) {
                bounded.Height = 24;
            }

            Children = [ 
                miniature,
                new LabelGeometry() {
                    Text = label,
                    TextSize = 16,
                    Paint = textPaint,
                    Padding = new Padding(8, 2, 0, 2),
                    VerticalAlign = Align.Start,
                    HorizontalAlign = Align.Start,
                }
            ];
        }

    }

}
