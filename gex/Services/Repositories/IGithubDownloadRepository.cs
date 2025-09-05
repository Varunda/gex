using gex.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public interface IGithubDownloadRepository {


        /// <summary>
        ///     download a folder from GitHub, placing it it in a folder but flattened (directory structure within the folder is not kept)
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task DownloadFolder(string folder, CancellationToken cancel);

        bool HasFile(string folder, string file);

        /// <summary>
        ///     get the contents of a file within a folder
        /// </summary>
        /// <param name="folder">name of the folder within the GitHub repo that will contain the file</param>
        /// <param name="file">name of the file</param>
        /// <param name="cancel">cancellation token</param>
        Task<Result<string, string>> GetFile(string folder, string file, CancellationToken cancel);

        /// <summary>
        ///     get a list of all files within a folder
        /// </summary>
        /// <param name="folder">name of the folder within the GitHub repo to get the name of the files of</param>
        /// <returns></returns>
        Result<List<string>, string> GetFiles(string folder);

    }
}