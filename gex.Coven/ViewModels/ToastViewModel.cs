using gex.Coven.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class ToastViewModel : ViewModelBase {

        public string Title { get; }

        public string Message { get; } 

        public string Type { get; }

        public ToastViewModel(string title, string message, ToastType type) {
            this.Title = title;
            this.Message = message;
            this.Type = type.ToString();
        }

    }
}
