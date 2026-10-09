import { BarMap } from "model/BarMap";
import ApiWrapper from "./ApiWrapper";
import { Loading } from "Loading";
import { BarMapData } from "model/BarMapData";


export class MapApi extends ApiWrapper<BarMap> {
    private static _instance: MapApi = new MapApi();
    public static get(): MapApi { return MapApi._instance; }

    public static getByFilename(filename: string): Promise<Loading<BarMap>> {
        return MapApi.get().readSingle(`/api/map/${encodeURIComponent(filename)}`, BarMap.parse);
    }

    public static getAll(): Promise<Loading<BarMap[]>> {
        return MapApi.get().readList(`/api/map/all`, BarMap.parse);
    }

    public static getMapData(mapFilename: string): Promise<Loading<BarMapData>> {
        return MapApi.get().readSingle(`/api/map/${mapFilename}/data`, BarMapData.parse);
    }

    public static updateStartSpotPositionRoleOverride(mapFilename: string, version: number, position: string, role: string, maxRadius: number | null): Promise<Loading<void>> {
        return MapApi.get().post(`/api/map/start-spot-position-role-override?mapFilename=${mapFilename}`
            + `&version=${version}&position=${position}&role=${encodeURIComponent(role)}`
            + `${maxRadius != null ? `&maxRadius=${maxRadius}` : ""}`);
    }

    public static recalculatePlayerStartSpots(mapFilename: string): Promise<Loading<void>> {
        return MapApi.get().post(`/api/map/${encodeURIComponent(mapFilename)}/recalculate-player-start-spots`);
    }

}