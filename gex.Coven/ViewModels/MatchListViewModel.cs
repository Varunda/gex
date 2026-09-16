using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Match;
using gex.Common.Services.Db;
using gex.Common.Services.Db.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Util;
using gex.Coven.Models.Config;
using gex.Coven.Models.Ui;
using gex.Coven.Services;
using gex.Coven.Services.Util;
using gex.Coven.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class MatchListViewModel : ViewModelBase {

        private readonly ILogger<MainViewModel> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarDemofileParser _Parser;
        private readonly UserOptionsService _UserOptionsService;
        private readonly DemofileWatcher _DemofileWatcher;
        private readonly IBarMatchBuilderUtil _MatchBuilder;
        private readonly BarMatchProcessorUtil _ProcessorUtil;
        private readonly BarMapParser _MapParser;
        private readonly IBarMapDb _MapDb;

        public MatchListViewModel() {
            _Logger = App.Current.Services.GetRequiredService<ILogger<MainViewModel>>();
            _MatchRepository = App.Current.Services.GetRequiredService<BarMatchRepository>();
            _Parser = App.Current.Services.GetRequiredService<BarDemofileParser>();
            _UserOptionsService = App.Current.Services.GetRequiredService<UserOptionsService>();
            _DemofileWatcher = App.Current.Services.GetRequiredService<DemofileWatcher>();
            _MatchBuilder = App.Current.Services.GetRequiredService<IBarMatchBuilderUtil>();
            _ProcessorUtil = App.Current.Services.GetRequiredService<BarMatchProcessorUtil>();
            _MapParser = App.Current.Services.GetRequiredService<BarMapParser>();
            _MapDb = App.Current.Services.GetRequiredService<IBarMapDb>();

            IObservable<Func<BarMatchViewModel, bool>> filterPredicate = 
                this.WhenAnyPropertyChanged(nameof(FilterMap), nameof(FilterPlayer), nameof(FilterGamemode))
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Select(CreateFilter);

            IObservable<IComparer<BarMatchViewModel>> sortFunction = 
                this.WhenAnyPropertyChanged(nameof(TableSortField), nameof(TableSortDirection))
                .Select(CreateSort);

            _Source.Connect()
                .Filter(filterPredicate)
                .Sort(
                    SortExpressionComparer<BarMatchViewModel>.Descending(match => match.StartTime),
                    comparerChanged: sortFunction
                ).Bind(out _Filtered)
                .Subscribe();

            Init();

            FilterMap = ""; // manually trigger a DynamicData change
        }

        public List<BarGamemodeItem> GamemodeList { get; } = [ ..BarGamemodeItem.Items ];

        private readonly SourceList<BarMatchViewModel> _Source = new();

        private readonly ReadOnlyObservableCollection<BarMatchViewModel> _Filtered;

        public ReadOnlyObservableCollection<BarMatchViewModel> Filtered => _Filtered;

        [ObservableProperty]
        private string? _FilterMap = null;

        [ObservableProperty]
        private string? _FilterPlayer = null;

        [ObservableProperty]
        private int? _FilterGamemode = null;

        [ObservableProperty]
        private MatchListSortField _TableSortField = MatchListSortField.StartTime;

        [ObservableProperty]
        private Models.Ui.SortDirection _TableSortDirection = Models.Ui.SortDirection.Desc;

        /// <summary>
        ///     init method that loads all matches from the repo
        /// </summary>
        private async void Init() {
            _DemofileWatcher.NewMatchReady += _DemofileWatcher_NewMatchReady;

            using CancellationTokenSource ctsLoadAll = new(TimeSpan.FromMinutes(10));
            _ = _DemofileWatcher.LoadAll(ctsLoadAll.Token);

            try {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(60));
                List<BarMatch> matches = await _MatchRepository.GetAll(cts.Token);

                foreach (BarMatch match in matches) {
                    Result<Maybe<BarMatch>, string> built = await _MatchBuilder.BuildMatch(match.ID, new IBarMatchBuilderUtil.BuildOptions() {
                        IncludeAiPlayers = true,
                        IncludeAllyTeams = true,
                        IncludePlayers = true,
                        IncludeTeams = true,
                    }, null, cts.Token);

                    if (built.IsOk == false) {
                        _Logger.LogError($"failed to build match [gameID={match.ID}] [error={built.Error}]");
                        AddMatch(match);
                    } else {
                        if (built.Value.Has()) {
                            AddMatch(built.Value.Get());
                        } else {
                            _Logger.LogWarning($"missing match from DB [gameID={match.ID}]");
                            AddMatch(match);
                        }
                    }
                }

            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed during init");
            }
        }

        /// <summary>
        ///     add a match to the view model
        /// </summary>
        /// <param name="match"></param>
        public void AddMatch(BarMatch match) {
            _Source.Add(new BarMatchViewModel(match));
        }

        /// <summary>
        ///     open a game, the parameter is the gameID as a string
        /// </summary>
        /// <param name="parameter">game ID as a string</param>
        /// <returns></returns>
        [RelayCommand]
        private async Task Open(object? parameter) {
            if (parameter is not string gameID) {
                return;
            }
            _Logger.LogInformation($"viewing match [gameID={gameID}]");

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            BarMatch? match = await _MatchRepository.GetByID(gameID, cts.Token);
            if (match == null) {
                _Logger.LogError($"failed to find match in Open command [gameID={gameID}]");
                return;
            }

            UserOptions userOptions = _UserOptionsService.Load();

            byte[] bytes = [];
            string replayFileName = Path.Join(userOptions.InstallFolder, "demos", match.FileName);
            if (File.Exists(match.FileName)) {
                bytes = File.ReadAllBytes(match.FileName);
            } else if (File.Exists(replayFileName)) {
                bytes = File.ReadAllBytes(replayFileName);
            } else {
                _Logger.LogError($"failed to find demofile [FileName={match.FileName}]");
                return;
            }

            Result<BarMatch, string> parsed = await _Parser.Parse(match.FileName, bytes, new DemofileParserOptions() {

            }, cts.Token);

            if (parsed.IsOk == false) {
                _Logger.LogError($"failed to parse match from demofile [gameID={gameID}] [error={parsed.Error}]");
                return;
            }

            BarMap? map = await _MapDb.GetByName(parsed.Value.Map, cts.Token);
            if (map == null) {
                _Logger.LogDebug($"map is not in DB, attempting to load from files [map={parsed.Value.Map}]");
                string mapName = parsed.Value.Map;
                string mapPath = Path.Join(userOptions.InstallFolder, "maps", mapName.Replace(" ", "_") + ".sd7");
                if (File.Exists(mapPath) == false) {
                    mapPath = Path.Join(userOptions.InstallFolder, "maps", mapName.ToLower().Replace(" ", "_") + ".sd7");
                }

                if (File.Exists(mapPath)) {
                    Result<BarMap, string> mapData = await _MapParser.Parse(mapPath, cts.Token);
                    if (mapData.IsOk == true) {
                        map = mapData.Value;

                        await _MapDb.Upsert(mapData.Value, cts.Token);
                        _Logger.LogInformation($"parsed map, saving to DB [map={mapName}]");
                    } else {
                        _Logger.LogError($"failed to parse map [map={map}] [mapDir={mapPath}] [error={mapData.Error}]");
                    }
                } else {
                    _Logger.LogWarning($"missing map directory [map={map}] [mapDir={mapPath}]");
                }
            }

            if (map == null) {
                _Logger.LogWarning($"failed to find map [map={parsed.Value.Map}]");
            }

            parsed.Value.MapData = map;

            MatchWindowViewModel vm = new(parsed.Value);

            MatchWindow win = new() {
                DataContext = vm
            };

            win.Show();
        }

        /// <summary>
        ///     event handler for when a new match is ready from the file watcher
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="match"></param>
        private async void _DemofileWatcher_NewMatchReady(object sender, BarMatch match) {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            BarMatch? existingMatch = await _MatchRepository.GetByID(match.ID, cts.Token);
            if (existingMatch != null) {
                _Logger.LogWarning($"match already exists [gameID={match.ID}]");
                return;
            }

            await _ProcessorUtil.Insert(match, cts.Token);

            AddMatch(match);
        }

        /// <summary>
        ///     sort by start time, or toggle direction if already sorting by start time
        /// </summary>
        [RelayCommand]
        public void TableSort_StartTime() {
            if (TableSortField == MatchListSortField.StartTime) {
                if (TableSortDirection == Models.Ui.SortDirection.Asc) {
                    TableSortDirection = Models.Ui.SortDirection.Desc;
                } else {
                    TableSortDirection = Models.Ui.SortDirection.Asc;
                }
            } else {
                TableSortField = MatchListSortField.StartTime;
            }
        }

        /// <summary>
        ///     sort by duration, or toggle direction if already sorting by direction
        /// </summary>
        [RelayCommand]
        public void TableSort_Duration() {
            if (TableSortField == MatchListSortField.Duration) {
                if (TableSortDirection == Models.Ui.SortDirection.Asc) {
                    TableSortDirection = Models.Ui.SortDirection.Desc;
                } else {
                    TableSortDirection = Models.Ui.SortDirection.Asc;
                }
            } else {
                TableSortField = MatchListSortField.Duration;
            }
        }

        /// <summary>
        ///     creates the filter used for the dynamic data list
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static Func<BarMatchViewModel, bool> CreateFilter(MatchListViewModel? model) {
            if (model == null) {
                return (_) => true;
            }

            if (string.IsNullOrWhiteSpace(model.FilterMap)
                && string.IsNullOrWhiteSpace(model.FilterPlayer)
                && model.FilterGamemode == null) {

                return (_) => true;
            }

            return (match) => {
                if (string.IsNullOrWhiteSpace(model.FilterMap) == false) {
                    if (match.Map.Contains(model.FilterMap, StringComparison.OrdinalIgnoreCase) == false) {
                        return false;
                    }
                }

                if (string.IsNullOrWhiteSpace(model.FilterPlayer) == false) {
                    List<string> teamNames = match.AllyTeams.SelectMany(iter => iter.Teams).Select(iter => iter.Name.ToLower()).ToList();

                    bool found = false;
                    foreach (string s in teamNames) {
                        if (s.Contains(model.FilterPlayer, StringComparison.OrdinalIgnoreCase) == true) {
                            found = true;
                            break;
                        }
                    }

                    if (found == false) {
                        return false;
                    }
                }

                if (model.FilterGamemode != null) {
                    if (match.GamemodeID != model.FilterGamemode) {
                        return false;
                    }
                }

                return true;
            };
        }

        private static IComparer<BarMatchViewModel> CreateSort(MatchListViewModel? model) {
            if (model == null) {
                return SortExpressionComparer<BarMatchViewModel>.Descending(iter => iter.StartTime);
            }

            if (model.TableSortField == MatchListSortField.StartTime) {
                if (model.TableSortDirection == Models.Ui.SortDirection.Asc) {
                    return SortExpressionComparer<BarMatchViewModel>.Ascending(iter => iter.StartTime);
                } 
                return SortExpressionComparer<BarMatchViewModel>.Descending(iter => iter.StartTime);
            } else if (model.TableSortField == MatchListSortField.Duration) {
                if (model.TableSortDirection == Models.Ui.SortDirection.Asc) {
                    return SortExpressionComparer<BarMatchViewModel>.Ascending(iter => iter.DurationMs);
                } 
                return SortExpressionComparer<BarMatchViewModel>.Descending(iter => iter.DurationMs);
            }

            throw new InvalidOperationException($"unchecked TableSortField [field={model.TableSortField}]");
        }

    }


}
