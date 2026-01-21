
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { ApiBarUnit, BarUnit } from "model/BarUnit";
import { BarUnitName } from "model/BarUnitName";

export class BarUnitApi extends ApiWrapper<BarUnit> {
    private static _instance: BarUnitApi = new BarUnitApi();
    public static get(): BarUnitApi { return BarUnitApi._instance; }

    public static getAll(): Promise<Loading<BarUnitName[]>> {
        return BarUnitApi.get().readList(`/api/unit/all`, BarUnitName.parse);
    }

    public static getAllDefinitions(): Promise<Loading<ApiBarUnit[]>> {
        return BarUnitApi.get().readList(`/api/unit/all-defs`, ApiBarUnit.parse);
    }

    public static getByDefinitionName(defName: string): Promise<Loading<ApiBarUnit>> {
        return BarUnitApi.get().readSingle(`/api/unit/def-name/${defName}`, ApiBarUnit.parse);
    }

}

