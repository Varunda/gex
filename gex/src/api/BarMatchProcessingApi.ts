
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { BarMatchProcessing } from "model/BarMatchProcessing";

export class BarMatchProcessingApi extends ApiWrapper<BarMatchProcessing> {
    private static _instance: BarMatchProcessingApi = new BarMatchProcessingApi();
    public static get(): BarMatchProcessingApi { return BarMatchProcessingApi._instance; }

    public static getPriorityList(): Promise<Loading<BarMatchProcessing[]>> {
        return BarMatchProcessingApi.get().readList(`/api/match-processing/priority`, BarMatchProcessing.parse);
    }

}