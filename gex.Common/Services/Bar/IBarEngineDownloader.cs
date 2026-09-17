
namespace gex.Common.Services.Bar {
    public interface IBarEngineDownloader {
        Task DownloadEngine(string version, CancellationToken cancel);
        bool HasEngine(string version);
    }
}