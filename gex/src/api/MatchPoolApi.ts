import { MatchPool } from "model/MatchPool";
import ApiWrapper from "./ApiWrapper";
import { Loading } from "Loading";
import { MatchPoolEntry } from "model/MatchPoolEntry";

export class MatchPoolApi extends ApiWrapper<MatchPool> {
    private static _instance: MatchPoolApi = new MatchPoolApi();
    public static get(): MatchPoolApi { return MatchPoolApi._instance; }

    public static getAll(): Promise<Loading<MatchPool[]>> {
        return MatchPoolApi.get().readList(`/api/match-pool`, MatchPool.parse);
    }

    public static getByID(poolID: number): Promise<Loading<MatchPool>> {
        return MatchPoolApi.get().readSingle(`/api/match-pool/${poolID}`, MatchPool.parse);
    }

    public static getEntriesByID(poolID: number): Promise<Loading<MatchPoolEntry[]>> {
        return MatchPoolApi.get().readList(`/api/match-pool/${poolID}/entries`, MatchPoolEntry.parse);
    }

    public static create(name: string): Promise<Loading<MatchPool>> {
        return MatchPoolApi.get().postReply(`/api/match-pool/?name=${encodeURIComponent(name)}`, MatchPool.parse);
    }

    public static addMatchToPool(poolID: number, matchID: string): Promise<Loading<void>> {
        return MatchPoolApi.get().post(`/api/match-pool/${poolID}/${matchID}`);
    }

    public static removeMatchFromPool(poolID: number, matchID: string): Promise<Loading<void>> {
        return MatchPoolApi.get().delete(`/api/match-pool/${poolID}/${matchID}`);
    }

}