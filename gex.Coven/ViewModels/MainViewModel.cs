using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Coven.Code;
using gex.Coven.Models;
using gex.Coven.Models.Config;
using gex.Coven.Services;
using gex.Coven.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class MainViewModel : ViewModelBase {

        public ToastService Toasts { get; }

        public DisplayLoggerService DisplayLogger { get; }

        public MainViewModel() {
            Toasts = App.Current.Services.GetService<ToastService>() ?? new ToastService();
            DisplayLogger = App.Current.Services.GetService<DisplayLoggerService>() ?? new DisplayLoggerService();
        }

        public void AddToast(string title, string message, ToastType type, TimeSpan duration) {
            Toasts.Show(title, message, type, duration);
        }

        [ObservableProperty]
        private string _Status = "Loading matches...";

        [RelayCommand]
        public void OpenLogs() {
            DisplayLoggerWindow win = new() {
                DataContext = DisplayLogger.Get()
            };

            win.Show();
        }

    }
}
