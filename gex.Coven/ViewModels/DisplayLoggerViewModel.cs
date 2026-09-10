using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using gex.Coven.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class DisplayLoggerViewModel : ObservableRecipient, IRecipient<DisplayLoggerMessage> {

        public DisplayLoggerViewModel() {
            IsActive = true;
        }

        public void Receive(DisplayLoggerMessage message) {
            if (Messages.Count > MaxMessages) {
                Messages.RemoveAt(Messages.Count - 1);
            }
            Messages.Insert(0, message);
        }

        [ObservableProperty]
        private ObservableCollection<DisplayLoggerMessage> _Messages = [];

        [ObservableProperty]
        private int _MaxMessages = 1000;

        [RelayCommand]
        public void Clear() {
            Messages.Clear();
        }

        [RelayCommand]
        public async Task OpenFolder() {
            TopLevel? tl = TopLevel.GetTopLevel(App.MainWindow);
            if (tl == null) {
                return;
            }

            DirectoryInfo di = new(Path.Join(Environment.CurrentDirectory, "logs"));
            if (Directory.Exists(di.FullName) == false) {
                return;
            }

            await tl.Launcher.LaunchDirectoryInfoAsync(di);
        }

    }
}
