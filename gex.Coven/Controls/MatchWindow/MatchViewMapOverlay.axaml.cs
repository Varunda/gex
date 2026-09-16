using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using gex.Common.Models.Map;
using gex.Common.Models.Match;
using gex.Coven.ViewModels;
using gex.Coven.ViewModels.Match;
using System;
using System.Globalization;

namespace gex.Coven.Controls.MatchWindow {

    public partial class MatchViewMapOverlay : UserControl {

        public MatchViewMapOverlay() {
            InitializeComponent();
        }

        private BarMap? _MapData = null;
        private double _MapWidth = 0;
        private double _MapHeight = 0;

        protected override void OnLoaded(RoutedEventArgs e) {
            base.OnLoaded(e);

            if (DataContext == null) {
                return;
            }

            if (DataContext is BarMatchViewModel bvm) {
                DataContext = new BarMatchMapViewModel(bvm.Match);
            }

            if (DataContext is not BarMatchMapViewModel vm) {
                throw new InvalidOperationException($"DataContext is not a {nameof(BarMatchMapViewModel)} [type={DataContext.GetType().Name}]");
            }

            _MapData = vm.Match.MapData;
            _MapWidth = _MapData?.Width * 512 ?? 1024;
            _MapHeight = _MapData?.Height * 512 ?? 1024;

            InvalidateVisual(); // redraw
        }

        public override void Render(DrawingContext context) {
            base.Render(context);

            if (DataContext is not BarMatchMapViewModel vm) {
                return;
            }

            RenderTeams(context, vm.Match);
        }

        #region render methods

        private void RenderTeams(DrawingContext context, BarMatch match) {
            foreach (BarMatchTeam team in match.Teams) {
                BarMatchTeamViewModel iter = new(match, team);

                Point center = new(ToImgX(team.StartingPosition.X), ToImgZ(team.StartingPosition.Z));
                context.DrawEllipse(iter.ColorBrush, null, center, 12, 12);

                DrawText(context, iter.Name, new Point(center.X + 12, center.Y - 12), iter.ColorBrush, center.X >= 512);

                if (team.StartSpotLabel != null) {
                    DrawText(context, team.StartSpotLabel, new Point(center.X + 12, center.Y - 36), iter.ColorBrush, center.X >= 512);
                }
            }
        }

        #endregion

        #region Helper methods

        /// <summary>
        ///     take a map coordinate and scale it to the 1024 width picture
        /// </summary>
        /// <param name="x"></param>
        private double ToImgX(double x) {
            return x / _MapWidth * 1024;
        }

        /// <summary>
        ///     take a map coordinate and scale it to the 1024 height picture
        /// </summary>
        /// <param name="x"></param>
        private double ToImgZ(double z) {
            return z / _MapHeight * 1024;
        }

        /// <summary>
        ///     draw text with an outline
        /// </summary>
        /// <param name="context"></param>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private static void DrawText(DrawingContext context, string text, Point position, IBrush color, bool offset = false, double size = 16d) {
            FormattedText ft = new(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Atkinson Hyperlegible Regular"),
                size,
                color
            );

            double w = ft.Width;

            if (offset == true) {
                position = position.WithX(position.X - w);
            }

            Geometry geo = ft.BuildGeometry(position)
                ?? throw new InvalidOperationException($"geometry for text was null");

            context.DrawGeometry(null, new Pen(Brushes.Black, 2), geo);
            context.DrawGeometry(color, null, geo);
        }

        #endregion

    }
}