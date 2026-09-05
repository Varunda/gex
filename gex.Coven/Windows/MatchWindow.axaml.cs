using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using gex.Coven.ViewModels;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Globalization;

namespace gex.Coven.Windows {

    public partial class MatchWindow : Window {

        public MatchWindow() {
            InitializeComponent();
        }

        private void TeamStatDropdown_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
            if (DataContext is not MatchWindowViewModel vm) {
                return;
            }

            if (e.AddedItems.Count < 1) {
                return;
            }

            if (e.AddedItems[0] is not string key) {
                return;
            }

            vm.SelectTeamStatsKey(key);
        }

    }
}