using gex.Code.ExtensionMethods;
using gex.Models.Db;
using gex.Services.Db;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Util {

	public class BarMatchPriorityCalculator {

		private readonly ILogger<BarMatchPriorityCalculator> _Logger;
		private readonly MapPriorityModDb _MapPriorityModDb;

		public BarMatchPriorityCalculator(ILogger<BarMatchPriorityCalculator> logger,
			MapPriorityModDb priorityModDb) {

			_Logger = logger;
			_MapPriorityModDb = priorityModDb;
		}

		public async Task<short> Calculate(BarMatch match, CancellationToken cancel) {
			short priority = -1;
			string why = "";

			MapPriorityMod? mapPrioMod = await _MapPriorityModDb.GetByName(match.MapName, cancel);

			// 6 player games that are not on maps with prio changes (metal maps)
			// are given prio and put into a different queue
			if (match.Players.Count <= 6 && mapPrioMod == null) {
				return priority;
			}

			priority = 10; // a bit of wiggle room for something idk

			if (mapPrioMod != null) {
				priority += mapPrioMod.Change;
				why += $"map {match.MapName} gives {mapPrioMod.Change}; ";
			}

			// de-prio low elo games
			double maxElo = match.Players.Select(iter => iter.Skill).Max();
			if (maxElo < 16) {
				priority += 30;
				why += $"low elo game (highest is {maxElo}); ";
			}

			// de-prio longer games (+4 prior per minute over 30)
			if (match.DurationMs > (1000 * 60 * 30)) {
				short minutesOver = (short) ((match.DurationMs - (1000 * 60 * 30)) / (1000 * 60));
				priority += (short)(minutesOver * 4);
				why += $"long game ({minutesOver} mins over 30); ";
			}

			// de-prio unranked games
			if (match.GameSettings.GetInt32("ranked_game", 0) == 0) {
				priority += 20;
				why += $"unranked game; ";
			}

			// games that tweak units are likely to last longer
			if (match.GameSettings.GetString("tweakunits", "") != "") {
				priority += 40;
				why += $"tweaked units; ";
			}
			
			// games that increase resource income
			if (match.GameSettings.GetString("multiplier_resourceincome", "1") != "1") {
				priority += 50;
				why += $"resource income changed; ";
			}

			// games that change default setting (buildpower)
			if (match.GameSettings.GetString("multiplier_buildpower", "1") != "1") {
				priority += 50;
				why += $"build power multiplied; ";
			}
			
			if (match.GameSettings.GetInt32("startmetal", 1000) != 1000) {
				priority += 50;
				why += $"starting metal changed; ";
			}

			if (match.GameSettings.GetInt32("startenergy", 1000) != 1000) {
				priority += 50;
				why += $"starting energy changed; ";
			}

			if (match.GameSettings.GetString("assistdronesenabled", "disabled") == "enabled") {
				priority += 10;
				why += $"assist drones enabled; ";
			}

			if (match.GameSettings.GetString("map_waterislava", "0") != "0") {
				priority += 30;
				why += $"water is lava; ";
			}

			if (priority < -1) {
				_Logger.LogWarning($"found a negative priority after calculating priority! assuming a wrap-around occured "
					+ $"[gameID={match.ID}] [priority={priority}] [why={why}]");
				priority = short.MaxValue;
			}

			_Logger.LogDebug($"game priority set [gameID={match.ID}] [priority={priority}] [why={why}]");

			return priority;
		}

	}
}
