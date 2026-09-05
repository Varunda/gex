using System.Threading.Tasks;

namespace gex.Common.Services.Db {

    /// <summary>
    /// Creates and updates the database
    /// </summary>
    public interface IDbCreator {

        /// <summary>
        /// Execute the creator
        /// </summary>
        Task Execute();

    }
}
