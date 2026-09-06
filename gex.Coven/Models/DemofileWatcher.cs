using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Coven.Models.Config;
using gex.Coven.Models.Match;
using gex.Coven.Services.Db.Match;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Models {

    public class DemofileWatcher {

        private readonly ILogger<DemofileWatcher> _Logger;
        private readonly BarDemofileParser _DemofileParser;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchHashDb _MatchHashDb;
        private readonly IOptions<UserOptions> _UserOptions;

        private readonly FileSystemWatcher _FileWatcher;

        public DemofileWatcher(ILogger<DemofileWatcher> logger,
            BarDemofileParser demofileParser, BarMatchRepository matchRepository,
            BarMatchHashDb matchHashDb, IOptions<UserOptions> userOptions) {

            _Logger = logger;

            _UserOptions = userOptions;
            _FileWatcher = new FileSystemWatcher(_UserOptions.Value.ReplayFolder);
            _FileWatcher.Filter = "*.sdfz";
            _FileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _FileWatcher.EnableRaisingEvents = true;
            _FileWatcher.Changed += FileWatcher_OnWrite;
            _DemofileParser = demofileParser;
            _MatchRepository = matchRepository;
            _MatchHashDb = matchHashDb;
        }

        public delegate void NewMatchReadyHandler(object sender, BarMatch match);
        public event NewMatchReadyHandler? NewMatchReady;

        private async void FileWatcher_OnWrite(object sender, FileSystemEventArgs args) {
            if (args.ChangeType != WatcherChangeTypes.Changed) {
                return;
            }

            FileInfo fi = new(args.FullPath);
            if (fi.Length == 0) {
                _Logger.LogInformation($"file is 0 size [file={args.FullPath}]");
                return;
            }

            _Logger.LogInformation($"new file [file={args.FullPath}]");

            if (args.Name == null) {
                Debug.Fail("why is args.Name null");
            }

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));

            try {
                byte[] bytes = await File.ReadAllBytesAsync(args.FullPath, cts.Token);
                Result<BarMatch, string> ret = await _DemofileParser.Parse(args.Name, bytes, new DemofileParserOptions() {

                }, cts.Token);

                if (ret.IsOk == false) {
                    _Logger.LogError($"failed to parse demofile [path={args.FullPath}] [error={ret.Error}]");
                    return;
                }

                NewMatchReady?.Invoke(this, ret.Value);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to parse demofile [path={args.FullPath}]");
            }

        }

        public async Task LoadAll() {
            string[] demofiles = Directory.GetFiles(_FileWatcher.Path, "*.sdfz");

            foreach (string demofile in demofiles) {
                FileInfo fi = new(demofile);
                if (fi.Length == 0) {
                    continue;
                }

                string filename = Path.GetFileName(demofile);

                byte[] bytes = await File.ReadAllBytesAsync(demofile);

                string md5 = string.Join("", MD5.HashData(bytes).Select(iter => iter.ToString("x2"))).ToLower();
                BarMatchHash? existingHash = await _MatchHashDb.GetByHash(md5, CancellationToken.None);
                if (existingHash != null) {
                    continue;
                }

                _Logger.LogInformation($"loading new match [file={demofile}]");

                try {
                    Result<BarMatch, string> ret = await _DemofileParser.Parse(filename, bytes, new DemofileParserOptions() {

                    }, CancellationToken.None);

                    if (ret.IsOk == true) {
                        await _MatchHashDb.Upsert(new BarMatchHash() {
                            GameID = ret.Value.ID,
                            FileName = filename,
                            Hash = md5
                        }, CancellationToken.None);

                        NewMatchReady?.Invoke(this, ret.Value);
                    } else {
                        _Logger.LogWarning($"failed to parse demofile [filename={filename}] [error={ret.Error}]");
                    }
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to parse demofile [path={demofile}]");
                }
            }
        }

    }
}
