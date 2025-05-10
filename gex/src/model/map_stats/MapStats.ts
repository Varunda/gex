import { MapStatsByGamemode } from "./MapStatsByGamemode";
import { MapStatsStartSpot } from "./MapStatsStartSpot";

export class MapStats {
    public mapFilename: string = "";
    public stats: MapStatsByGamemode[] = [];
    public startSpots: MapStatsStartSpot[] = [];

    public static parse(elem: any): MapStats {
        return {
            mapFilename: elem.mapFilename,
            stats: elem.stats.map((iter: any) => MapStatsByGamemode.parse(iter)),
            startSpots: elem.startSpots.map((iter: any) => MapStatsStartSpot.parse(iter))
        }
    }

}