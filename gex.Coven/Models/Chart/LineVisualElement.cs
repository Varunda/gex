using Avalonia.Platform;
using gex.Coven.Code;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Drawing.Layouts;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.VisualElements;
using SkiaSharp;
using System.IO;

namespace gex.Coven.Models.Chart {

    public class LineVisualElement : Visual {

        private static int _Index { get; set; } = 0;

        protected override AbsoluteLayout DrawnElement { get; }

        public LineVisualElement(double frame, Paint fill, string text) {
            this.Frame = frame;
            this.Text = text;
            this.Fill = fill;

            _Text = new LabelGeometry() {
                Text = text,
                TextSize = 16,
                HorizontalAlign = Align.Middle,
                Y = 100 + ((_Index++ % 8) * 40),
                Paint = fill,
                Padding = new Padding(4)
            };

            LvcSize textSize = _Text.Measure();

            _Line = new RectangleGeometry() {
                Height = 1000,
                Width = 2,
                Fill = fill,
            };

            _LabelBackground = new RoundedRectangleGeometry() {
                Fill = new SolidColorPaint(SKColors.Black),
                Height = textSize.Height + 8,
                Width = textSize.Width + 8,
                Y = (_Text.Y - textSize.Height / 2) - 4,
                X = (_Text.X - textSize.Width / 2) - 4,
                BorderRadius = new LvcPoint(8, 8),
                Stroke = new SolidColorPaint(SKColors.DimGray),
                StrokeThickness = 1f
            };

            DrawnElement = new AbsoluteLayout() {
                Children = [
                    _Line,
                    _LabelBackground,
                    _Text,
                ],
            };
        }

        public string Text { get; set; } = "";

        public double Frame { get; set; }

        public Paint Fill { get; set; }

        private readonly LabelGeometry _Text;
        private readonly RectangleGeometry _Line;
        private readonly RoundedRectangleGeometry _LabelBackground;

        protected override void Measure(LiveChartsCore.Chart chart) {
            ICartesianChartView port = (ICartesianChartView)chart.View;

            LvcPointD scale = new(Frame, 0);
            LvcPointD loc = port.ScaleDataToPixels(scale);

            DrawnElement.X = (float)loc.X;
            DrawnElement.Y = 0;
            DrawnElement.Height = 10000;
            DrawnElement.Width = 2;

            _Line.Height = port.ControlSize.Height;
        }

    }
}
