using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Coven.Code;
using gex.Coven.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services {

    public partial class ToastService : ObservableObject {

        public ObservableCollection<ToastViewModel> Toasts { get; } = [];

        public void Show(string message, ToastType type, TimeSpan duration) {
            ToastViewModel vm = new(message, type);

            Toasts.Add(vm);

            if (duration.Ticks > 0) {
                Task.Delay(duration).ContinueWith((Task _) => {
                    Dispatcher.UIThread.Post(() => {
                        Toasts.Remove(vm);
                    });
                });
            }
        }

        [RelayCommand]
        private void Dismiss(ToastViewModel vm) {
            Trace.WriteLine($"removing vm [message={vm.Message}]");
            Toasts.Remove(vm);
        }

    }
}
