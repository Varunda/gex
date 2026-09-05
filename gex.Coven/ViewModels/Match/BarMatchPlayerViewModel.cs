using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Models.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels.Match {

    public partial class BarMatchPlayerViewModel : ViewModelBase {

        public BarMatchPlayerViewModel() {

        }

        public BarMatchPlayerViewModel(BarMatch match, BarMatchPlayer player) {
            _Name = player.Name;
            _UserID = player.UserID;
            _Skill = player.Skill;
        }

        [ObservableProperty]
        private string _Name = "";

        [ObservableProperty]
        private long? _UserID = null;

        [ObservableProperty]
        private double? _Skill = null;

    }
}
