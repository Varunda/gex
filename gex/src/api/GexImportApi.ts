import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class GexImportApi extends ApiWrapper<string> {
    private static _instance: GexImportApi = new GexImportApi();
    public static get(): GexImportApi { return GexImportApi._instance; }

    public static match(gexUrl: string, gameID: string): Promise<Loading<void>> {
        return GexImportApi.get().post(`/api/gex-import/match/${gameID}?gexUrl=${encodeURIComponent(gexUrl)}`);
    }


}