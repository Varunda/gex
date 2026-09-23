using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Util;
using gex.Coven.Models.Config;
using gex.Coven.Services;
using gex.Coven.Services.Util;
using gex.Coven.ViewModels;
using Huskui.Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Windows {

    public partial class MatchWindow : AppWindow {

        public MatchWindow() {
            InitializeComponent();
        }


        /// <summary>
        ///     helper method to open a match window
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public static async Task LoadMatchAndShow(string gameID, CancellationToken cancel) {
            ILogger<MatchWindow> logger = App.Current.Services.GetRequiredService<ILogger<MatchWindow>>();
            BarMatchRepository matchRepository = App.Current.Services.GetRequiredService<BarMatchRepository>();
            UserOptionsService userOptionsService = App.Current.Services.GetRequiredService<UserOptionsService>();
            BarDemofileParser parser = App.Current.Services.GetRequiredService<BarDemofileParser>();
            IBarMatchBuilderUtil matchBuilder = App.Current.Services.GetRequiredService<IBarMatchBuilderUtil>();

            logger.LogInformation($"viewing match [gameID={gameID}]");

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));

            BarMatch? match = await matchRepository.GetByID(gameID, cts.Token);
            if (match == null) {
                logger.LogError($"failed to find match in Open command [gameID={gameID}]");
                return;
            }

            UserOptions userOptions = userOptionsService.Load();

            byte[] bytes = [];
            string replayFileName = Path.Join(userOptions.InstallFolder, "demos", match.FileName);
            if (File.Exists(match.FileName)) {
                bytes = File.ReadAllBytes(match.FileName);
            } else if (File.Exists(replayFileName)) {
                bytes = File.ReadAllBytes(replayFileName);
            } else {
                logger.LogError($"failed to find demofile [FileName={match.FileName}]");
                return;
            }

            Result<BarMatch, string> parsed = await parser.Parse(match.FileName, bytes, new DemofileParserOptions() {

            }, cts.Token);

            if (parsed.IsOk == false) {
                logger.LogError($"failed to parse match from demofile [gameID={gameID}] [error={parsed.Error}]");
                return;
            }

            Result<Maybe<BarMatch>, string> builtMatch = await matchBuilder.BuildMatch(gameID, new IBarMatchBuilderUtil.BuildOptions() {
                IncludeMapData = true,
            }, null, cts.Token);

            if (builtMatch.IsOk && builtMatch.Value.Has()) {
                parsed.Value.MapData = builtMatch.Value.Get().MapData;
            }

            if (parsed.Value.MapData == null) {
                logger.LogWarning($"failed to find map [map={parsed.Value.Map}]");
            }

            MatchWindowViewModel vm = new(parsed.Value);

            MatchWindow win = new() {
                DataContext = vm
            };

            WindowManager.Register(win);

            win.Show();
        }

    }
}