import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { MapStats } from "model/map_stats/MapStats";
import { MapStatsByGamemode } from "model/map_stats/MapStatsByGamemode";
import { MapStatsStartSpot } from "model/map_stats/MapStatsStartSpot";

export class MapStatsApi extends ApiWrapper<MapStatsByGamemode> {
    private static _instance: MapStatsApi = new MapStatsApi();
    public static get(): MapStatsApi { return MapStatsApi._instance; }

    public static getByMapFilename(filename: string): Promise<Loading<MapStats>> {
        return MapStatsApi.get().readSingle(`/api/map-stats/${encodeURIComponent(filename)}?includeStats=true&includeStartSpots=true&includeFactionStats=true&includeOpeningLabs=true`, MapStats.parse);
    }

    public static getStartSpotsByMapAndUser(mapName: string, userID: number): Promise<Loading<MapStatsStartSpot[]>> {
        return MapStatsApi.get().readList(`/api/user/start-spots/${userID}?mapName=${encodeURIComponent(mapName)}`, MapStatsStartSpot.parse);
    }

}