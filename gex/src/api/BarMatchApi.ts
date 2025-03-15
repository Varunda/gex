import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { BarMatch } from "model/BarMatch";

export class BarMatchApi extends ApiWrapper<BarMatch> {
    private static _instance: BarMatchApi = new BarMatchApi();
    public static get(): BarMatchApi { return BarMatchApi._instance; }

    public static getByID(gameID: string): Promise<Loading<BarMatch>> {
        return BarMatchApi.get().readSingle(`/api/match/${gameID}?includePlayers=true&includeAllyTeams=true&includeSpectators=true&includeChat=true`, BarMatch.parse);
    }

    public static getRecent(offset: number = 0, limit: number = 24): Promise<Loading<BarMatch[]>> {
        return BarMatchApi.get().readList(`/api/match/recent?offset=${offset}&limit=${limit}`, BarMatch.parse);
    }


}