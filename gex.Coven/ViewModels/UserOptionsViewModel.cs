using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Coven.Models.Config;
using gex.Coven.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class UserOptionsViewModel : ViewModelBase {

        private readonly ILogger<UserOptionsViewModel> _Logger;
        private readonly UserOptionsService _UserOptionsService;

        private readonly UserOptions _UserOptions;

        public UserOptionsViewModel(ILogger<UserOptionsViewModel> logger,
            UserOptionsService userOptionsService) {

            _Logger = logger;
            _UserOptionsService = userOptionsService;

            _UserOptions = _UserOptionsService.Load();

            _ReplayFolder = _UserOptions.ReplayFolder;
        }

        [ObservableProperty]
        private string _ReplayFolder = "";

        [RelayCommand]
        public async Task OpenReplayFolderDialog() {
            TopLevel? tl = TopLevel.GetTopLevel(App.MainWindow);
            if (tl == null) {
                return;
            }

            if (tl.StorageProvider.CanPickFolder == false) {
                return;
            }

            IReadOnlyList<IStorageFolder> folders = await tl.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions() {
                AllowMultiple = false,
                Title = "Pick the folder that contains all the demofiles (.sdfz files)",
            });

            if (folders.Count < 1) {
                return;
            }

            ReplayFolder = folders[0].Path.AbsolutePath;
            _UserOptions.ReplayFolder = ReplayFolder;
            _UserOptionsService.Save(_UserOptions);
        }

    }
}
