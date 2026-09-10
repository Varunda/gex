using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using gex.Coven.ViewModels;

namespace gex.Coven.Views {
    public partial class MatchListView : UserControl {

        public MatchListView() {
            InitializeComponent();
            this.DataContext = new MatchListViewModel();
        }

    }
}