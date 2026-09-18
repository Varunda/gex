using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class DemofileLaunchReplayViewModel : ViewModelBase {

        private readonly ILogger<DemofileLaunchReplayViewModel> _Logger;

        private CancellationTokenSource _CancelSource;

        public DemofileLaunchReplayViewModel() {
            _Logger = App.Current?.Services?.GetService<ILogger<DemofileLaunchReplayViewModel>>() ?? default!;

            _CancelSource = new CancellationTokenSource();

            try {
                _ = Init(_CancelSource.Token);
            } catch (Exception ex) {

            }
        }

        public async Task Init(CancellationToken cancel) {

        }

        [RelayCommand]
        public void CancelLaunch() {
            _CancelSource.Cancel();
        }

    }
}
