import { BarMap } from "model/BarMap";
import ApiWrapper from "./ApiWrapper";
import { Loading } from "Loading";


export class MapApi extends ApiWrapper<BarMap> {
    private static _instance: MapApi = new MapApi();
    public static get(): MapApi { return MapApi._instance; }

    public static getByFilename(filename: string): Promise<Loading<BarMap>> {
        return MapApi.get().readSingle(`/api/map/${encodeURIComponent(filename)}`, BarMap.parse);
    }

}