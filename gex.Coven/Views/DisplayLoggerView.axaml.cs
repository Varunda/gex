using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using gex.Coven.Services;
using gex.Coven.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Coven.Views {
    public partial class DisplayLoggerView : UserControl {

        public DisplayLoggerView() {
            InitializeComponent();
            this.DataContext = App.Current?.Services?.GetService<DisplayLoggerService>()?.Get() ?? new DisplayLoggerViewModel();
        }

    }
}