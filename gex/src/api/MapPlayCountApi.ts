
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { MapPlayCountEntry } from "model/MapPlayCountEntry";

export class MapPlayCountApi extends ApiWrapper<MapPlayCountEntry> {
    private static _instance: MapPlayCountApi = new MapPlayCountApi();
    public static get(): MapPlayCountApi { return MapPlayCountApi._instance; }

    public static getRecent(): Promise<Loading<MapPlayCountEntry[]>> {
        return MapPlayCountApi.get().readList(`/api/map-play-count/recent`, MapPlayCountEntry.parse);
    }

}