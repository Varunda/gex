

import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { BarMapRotation } from "model/BarMapRotation";

export class BarMapRotationApi extends ApiWrapper<BarMapRotation> {
    private static _instance: BarMapRotationApi = new BarMapRotationApi();
    public static get(): BarMapRotationApi { return BarMapRotationApi._instance; }

    public static getAll(): Promise<Loading<BarMapRotation[]>> {
        return BarMapRotationApi.get().readList(`/api/map-rotation/`, BarMapRotation.parse);
    }

}