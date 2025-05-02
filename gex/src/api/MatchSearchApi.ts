
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { SearchResult } from "model/SearchResult";

export class MatchSearchApi extends ApiWrapper<string> {
    private static _instance: MatchSearchApi = new MatchSearchApi();
    public static get(): MatchSearchApi { return MatchSearchApi._instance; }

    public static getUniqueEngines(): Promise<Loading<SearchResult[]>> {
        return MatchSearchApi.get().readList(`/api/match-search/engines`, SearchResult.parse);
    }

    public static getUniqueGameVersions(): Promise<Loading<SearchResult[]>> {
        return MatchSearchApi.get().readList(`/api/match-search/game-versions`, SearchResult.parse);
    }

    public static getUniqueMaps(): Promise<Loading<SearchResult[]>> {
        return MatchSearchApi.get().readList(`/api/match-search/maps`, SearchResult.parse);
    }

}