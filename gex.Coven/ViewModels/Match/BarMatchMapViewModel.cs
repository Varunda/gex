using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Models.Match;
using HarfBuzzSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels.Match {

    public partial class BarMatchMapViewModel : ViewModelBase {

        public BarMatchMapViewModel() { }

        public BarMatchMapViewModel(BarMatch match) {
            _Map = match.Map;
            _Match = match;
        }

        [ObservableProperty]
        private string _Map = "";

        [ObservableProperty]
        private BarMatch _Match = new();

    }
}
