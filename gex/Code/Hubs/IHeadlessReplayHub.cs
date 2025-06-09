using gex.Models.Api;
using System.Threading.Tasks;

namespace gex.Code.Hubs {

    public interface IHeadlessReplayHub {

        Task UpdateProgress(HeadlessRunStatus status);

        Task Finish();

    }
}
