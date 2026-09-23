using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using gex.Coven.ViewModels;
using Huskui.Avalonia.Controls;
using System;

namespace gex.Coven.Windows {
    public partial class HeadlessReplayWindow : AppWindow {

        public HeadlessReplayWindow() {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e) {
            base.OnLoaded(e);

            if (DataContext == null) {
                return;
            }

            if (DataContext is not HeadlessReplayViewModel vm) {
                throw new InvalidOperationException($"DataContext for {nameof(HeadlessReplayWindow)} is the wrong type of {DataContext.GetType().Name}");
            }

            vm.OnRequestClose += (object? sender, EventArgs args) => {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    this.Close();
                });
            };
        }

    }
}