import { Loading } from "Loading";
import ApiWrapper from "./ApiWrapper";
import { UnitIconAtlasJson } from "model/UnitIconAtlasData";

export class UnitIconAtlasOffsetApi extends ApiWrapper<string> {
    private static _instance: UnitIconAtlasOffsetApi = new UnitIconAtlasOffsetApi();
    public static get(): UnitIconAtlasOffsetApi { return UnitIconAtlasOffsetApi._instance }

    public static async getAll(): Promise<Loading<UnitIconAtlasJson>> {
        return UnitIconAtlasOffsetApi.get().readSingle(`/image-proxy/UnitIconAtlasJson`, UnitIconAtlasJson.parse);
    }
    
}