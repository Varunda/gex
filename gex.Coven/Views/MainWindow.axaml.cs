using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using gex.Coven.Services;
using gex.Coven.Services.Util;
using Huskui.Avalonia.Controls;
using System.IO;

namespace gex.Coven.Views;

public partial class MainWindow : AppWindow {

    public MainWindow() {
        InitializeComponent();

        WindowManager.Register(this);
    }

}