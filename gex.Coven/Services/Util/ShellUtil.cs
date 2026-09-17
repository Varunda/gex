using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Util {

    public static class ShellUtil {

        /// <summary>
        ///     get the directory that the application is located in, and where the db, cache, and UserOptions are stored
        /// </summary>
        /// <returns></returns>
        public static string GetWorkingDirectory() {
            return Path.GetDirectoryName(Environment.ProcessPath)!;
        }

    }
}
