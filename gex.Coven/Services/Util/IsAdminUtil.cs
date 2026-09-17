using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Util {

    public class IsAdminUtil {


        public static bool IsAdmin() {
            if (OperatingSystem.IsWindows()) {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            } else {
                return false;
            }
        }

    }
}
