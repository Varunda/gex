using CommunityToolkit.Mvvm.ComponentModel;
using gex.Coven.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services {

    public partial class DisplayLoggerService : ViewModelBase {

        private readonly DisplayLoggerViewModel _ViewModel = new();

        public DisplayLoggerService() {
            _ViewModel.PropertyChanged += _ViewModel_PropertyChanged;
        }

        public DisplayLoggerViewModel Get() {
            return _ViewModel;
        }

        [ObservableProperty]
        private int _MessageCount = 0;

        private void _ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DisplayLoggerViewModel.Messages)) {
                MessageCount = _ViewModel.Messages.Count;
            }
        }

    }
}
