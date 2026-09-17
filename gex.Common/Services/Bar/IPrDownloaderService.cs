
namespace gex.Common.Services.Bar {
    public interface IPrDownloaderService {
        Task<bool> GetGameVersion(string engine, string version, CancellationToken cancel);
        Task GetMap(string engine, string mapName, CancellationToken cancel);
        bool HasGameVersion(string engine, string version);
        bool HasMap(string engine, string map);
    }
}