import { MapDailyPlays } from "./MapDailyPlays";
import { MapPositionLeaderboardEntry } from "./MapPositionLeaderboardEntry";
import { MapStatsByFaction } from "./MapStatsByFaction";
import { MapStatsByGamemode } from "./MapStatsByGamemode";
import { MapStatsDailyOpeningLab } from "./MapStatsDailyOpeningLab";
import { MapStatsOpeningLab } from "./MapStatsOpeningLab";
import { MapStatsStartSpot } from "./MapStatsStartSpot";

export class MapStats {
    public mapFilename: string = "";
    public stats: MapStatsByGamemode[] = [];
    public startSpots: MapStatsStartSpot[] = [];
    public factionStats: MapStatsByFaction[] = [];
    public openingLabs: MapStatsDailyOpeningLab[] = [];
    public dailyPlays: MapDailyPlays[] = [];
    public positionLeaderboard: MapPositionLeaderboardEntry[] = [];

    public static parse(elem: any): MapStats {
        return {
            mapFilename: elem.mapFilename,
            stats: elem.stats.map((iter: any) => MapStatsByGamemode.parse(iter)),
            startSpots: elem.startSpots.map((iter: any) => MapStatsStartSpot.parse(iter)),
            factionStats: elem.factionStats.map((iter: any) => MapStatsByFaction.parse(iter)),
            openingLabs: elem.openingLabs.map((iter: any) => MapStatsDailyOpeningLab.parse(iter)),
            dailyPlays: elem.dailyPlays.map((iter: any) => MapDailyPlays.parse(iter)),
            positionLeaderboard: elem.positionLeaderboard.map((iter: any) => MapPositionLeaderboardEntry.parse(iter))
        };
    }

}