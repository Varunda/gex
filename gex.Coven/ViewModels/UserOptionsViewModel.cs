using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotnetFileAssociator;
using gex.Coven.Code;
using gex.Coven.Models.Config;
using gex.Coven.Models.Match;
using gex.Coven.Services;
using gex.Coven.Services.Db.Match;
using gex.Coven.Services.Util;
using ImageMagick.Drawing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class UserOptionsViewModel : ViewModelBase {

        private readonly ILogger<UserOptionsViewModel> _Logger;
        private readonly UserOptionsService _UserOptionsService;
        private readonly ToastService _ToastService;
        private readonly BarMatchIgnoredFilesDb _IgnoredFilesDb;

        private readonly UserOptions _UserOptions;

        public UserOptionsViewModel() {
            _Logger = default!;
            _UserOptionsService = default!;
            _ToastService = default!;
            _UserOptions = default!;
            _IgnoredFilesDb = default!;
            _IsAdmin = false;
        }

        public UserOptionsViewModel(ILogger<UserOptionsViewModel> logger,
            UserOptionsService userOptionsService, ToastService toastService,
            BarMatchIgnoredFilesDb ignoredFilesDb) {

            _Logger = logger;
            _UserOptionsService = userOptionsService;
            _ToastService = toastService;
            _IgnoredFilesDb = ignoredFilesDb;

            _UserOptions = _UserOptionsService.Load();
            _IsAdmin = IsAdminUtil.IsAdmin();

            _InstallFolder = _UserOptions.InstallFolder;
            _VersionCheckUpdates = _UserOptions.CheckForUpdates;
            _VersionAutoUpdate = _UserOptions.AutoUpdate;

            Init();
        }

        [ObservableProperty]
        private string _InstallFolder = "";

        [ObservableProperty]
        private ObservableCollection<BarMatchIgnoredFile> _IgnoredFiles = [];

        [ObservableProperty]
        private bool _SdfzAssociated = false;

        [ObservableProperty]
        private bool _IsAdmin;

        [ObservableProperty]
        private bool _VersionCheckUpdates;

        [ObservableProperty]
        private bool _VersionAutoUpdate;

        private async void Init() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
            List<BarMatchIgnoredFile> ignored = await _IgnoredFilesDb.GetAll(cts.Token);
            IgnoredFiles = new ObservableCollection<BarMatchIgnoredFile>(ignored);

            string appLoc = Environment.ProcessPath ?? "";
            SdfzAssociated = FileAssociator.IsFileAssociationSet(appLoc, ".sdfz");
        }

        /// <summary>
        ///     command to open a folder picker to pick the install folder
        /// </summary>
        /// <returns></returns>
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
                Title = "Select the Beyond all Reason install directory",
            });

            if (folders.Count < 1) {
                return;
            }

            string installDir = folders[0].Path.AbsolutePath;
            if (Directory.Exists(Path.Join(installDir, "demos")) == false) {
                _ToastService.Show($"Cannot set install folder", $"Missing 'demos' folder from '{installDir}'", Code.ToastType.ERROR, TimeSpan.FromSeconds(-1));
                return;
            }

            if (Directory.Exists(Path.Join(installDir, "maps")) == false) {
                _ToastService.Show($"Cannot set install folder", $"Missing 'maps' folder from '{installDir}'", Code.ToastType.ERROR, TimeSpan.FromSeconds(-1));
                return;
            }

            InstallFolder = installDir;
            _UserOptions.InstallFolder = InstallFolder;
            _UserOptionsService.Save(_UserOptions);

            _ToastService.Show("Updated install folder", "Install folder updated and saved", ToastType.INFO, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        ///     command to open the selected install folder
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        public async Task OpenReplayFolderExplorer() {
            TopLevel? tl = TopLevel.GetTopLevel(App.MainWindow);
            if (tl == null) {
                return;
            }

            if (Directory.Exists(InstallFolder) == false) {
                return;
            }

            DirectoryInfo di = new(InstallFolder);
            await tl.Launcher.LaunchDirectoryInfoAsync(di);
        }

        /// <summary>
        ///     remove a specific ignored file
        /// </summary>
        [RelayCommand]
        public async Task RemoveIgnoredFile(BarMatchIgnoredFile ignored) {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
            await _IgnoredFilesDb.Remove(ignored.FileName, cts.Token);

            _ToastService.Show("Success",
                $"Unignored file '{ignored.FileName}'",
                ToastType.INFO,
                TimeSpan.FromSeconds(5)
            );

            List<BarMatchIgnoredFile> list = await _IgnoredFilesDb.GetAll(cts.Token);
            IgnoredFiles = new ObservableCollection<BarMatchIgnoredFile>(list);
        }

        [RelayCommand]
        public void AddFileAssociation() {
            string? appLoc = Environment.ProcessPath;
            if (appLoc == null) {
                return;
            }

            try {
                FileAssociator.SetFileAssociation(appLoc, ".sdfz");
                _ToastService.Show("Success", ".sdfz will now open in gex.Coven", ToastType.INFO, TimeSpan.FromSeconds(5));
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to add file association to .sdfz files");
                _ToastService.Show("Error", $"{ex.Message}", ToastType.ERROR, TimeSpan.FromSeconds(5));
            }
        }

        [RelayCommand]
        public void RemoveFileAssociation() {
            string? appLoc = Environment.ProcessPath;
            if (appLoc == null) {
                return;
            }

            try {
                FileAssociator.RemoveFileAssociation(appLoc, ".sdfz");
                _ToastService.Show("Success", ".sdfz will not open in gex.Coven", ToastType.INFO, TimeSpan.FromSeconds(5));
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to remove file association to .sdfz files");
                _ToastService.Show("Error", $"{ex.Message}", ToastType.ERROR, TimeSpan.FromSeconds(5));
            }
        }

    }
}
