using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using gex.Common.Models.Match;
using gex.Coven.ViewModels;
using gex.Coven.ViewModels.Match;
using System;

namespace gex.Coven.Controls.MatchWindow {
    public partial class MatchViewMap : UserControl {

        public MatchViewMap() {
            InitializeComponent();
        }

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
        }

    }
}