using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Util {

    public class WindowManager {

        private static readonly List<Window> _Windows = [];

        public static void Register(Window window) {
            _Windows.Add(window);

            window.Closed += (_, _) => {
                _Windows.Remove(window);
            };
        }

        public static void CloseAll() {
            // take copy cause closing the window will modify the list
            foreach (Window window in _Windows.ToList()) {
                window.Close();
            }
        }

    }
}
