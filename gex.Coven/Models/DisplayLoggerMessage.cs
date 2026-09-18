using Avalonia.Media;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models {

    public class DisplayLoggerMessage {

        public DateTime Timestamp { get; set; }

        public string Level { get; set; } = "info";

        public IBrush BackgroundColor { get; set; } = Brushes.White;

        public IBrush Foreground { get; set; } = Brushes.Black;

        public string Category { get; set; } = "";

        public string Message { get; set; } = "";

    }
}
