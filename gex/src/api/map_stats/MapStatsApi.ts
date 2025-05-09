import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { MapStatsByGamemode } from "model/map_stats/MapStatsByGamemode";

export class MapStatsApi extends ApiWrapper<MapStatsByGamemode> {
    private static _instance: MapStatsApi = new MapStatsApi();
    public static get(): MapStatsApi { return MapStatsApi._instance; }

    public static getByMapFilename(filename: string): Promise<Loading<MapStatsByGamemode[]>> {
        return MapStatsApi.get().readList(`/api/map-stats/${encodeURIComponent(filename)}`, MapStatsByGamemode.parse);
    }

}