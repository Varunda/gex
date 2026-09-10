using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using gex.Coven.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace gex.Coven.Views {
    public partial class UserOptionsView : UserControl {

        public UserOptionsView() {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetRequiredService<UserOptionsViewModel>();
        }

    }
}