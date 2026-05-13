
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { ApiBarUnit, BarUnit } from "model/BarUnit";
import { BarUnitName } from "model/BarUnitName";
import { UserUnitsMaderLeaderboardEntry } from "model/UserUnitsMadeLeaderboardEntry";

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

    public static getLeaderboard(options: UserUnitsMadeLeaderboardOptions): Promise<Loading<UserUnitsMaderLeaderboardEntry[]>> {
        const params: URLSearchParams = new URLSearchParams();
        for (const i of options.defNames) { params.append("unitDefs", i); }
        params.set("periodStart", options.periodStart.toISOString());
        params.set("periodEnd", options.periodEnd.toISOString());
        params.set("offset", options.offset.toString());
        params.set("limit", options.limit.toString());

        for (const i of options.mapsFilenames ?? []) { params.append("mapFilenames", i); }
        for (const i of options.gamemodes ?? [] ) { params.append("gamemodes", i.toString()); }

        return BarUnitApi.get().readList(`/api/unit/leaderboard?${params.toString()}`, UserUnitsMaderLeaderboardEntry.parse);
    }

}

export interface UserUnitsMadeLeaderboardOptions {
    defNames: string[];
    periodStart: Date;
    periodEnd: Date;
    offset: number;
    limit: number;
    gamemodes?: number[];
    mapsFilenames?: string[];
    userIDs?: number[];
}