
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { MapPlayCountEntry } from "model/MapPlayCountEntry";

export class MapPlayCountApi extends ApiWrapper<MapPlayCountEntry> {
    private static _instance: MapPlayCountApi = new MapPlayCountApi();
    public static get(): MapPlayCountApi { return MapPlayCountApi._instance; }

    public static getDaily(): Promise<Loading<MapPlayCountEntry[]>> {
        return MapPlayCountApi.get().readList(`/api/map-play-count/recent/daily`, MapPlayCountEntry.parse);
    }

    public static get7Day(): Promise<Loading<MapPlayCountEntry[]>> {
        return MapPlayCountApi.get().readList(`/api/map-play-count/recent/7day`, MapPlayCountEntry.parse);
    }

    public static get30Day(): Promise<Loading<MapPlayCountEntry[]>> {
        return MapPlayCountApi.get().readList(`/api/map-play-count/recent/30day`, MapPlayCountEntry.parse);
    }

}