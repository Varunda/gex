import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { BarMatch, SearchKeyValue } from "model/BarMatch";
import { SearchOptions } from "model/SearchOptions";

export class BarMatchApi extends ApiWrapper<BarMatch> {
    private static _instance: BarMatchApi = new BarMatchApi();
    public static get(): BarMatchApi { return BarMatchApi._instance; }

    public static getByID(gameID: string): Promise<Loading<BarMatch>> {
        return BarMatchApi.get().readSingle(
            `/api/match/${gameID}?includePlayers=true&includeAllyTeams=true&includeSpectators=true&includeChat=true&includeTeamDeaths=true&includeLabeledPings=true&includePlayerLeaves=true`,
            BarMatch.parse
        );
    }

    public static getRecent(offset: number = 0, limit: number = 24): Promise<Loading<BarMatch[]>> {
        return BarMatchApi.get().readList(`/api/match/recent?offset=${offset}&limit=${limit}`, BarMatch.parse);
    }

    public static getByUserID(userID: number): Promise<Loading<BarMatch[]>> {
        return BarMatchApi.get().readList(`/api/match/user/${userID}`, BarMatch.parse);
    }

    public static getStdout(gameID: string): Promise<Loading<string>> {
        return BarMatchApi.get().readSingle(`/api/match/${gameID}/stdout`, (elem) => elem as string);
    }

    public static search(
        offset: number = 0, limit: number = 24,
        orderBy: string, orderByDir: string,
        options: SearchOptions
    ): Promise<Loading<BarMatch[]>> {
        const search: URLSearchParams = this.buildSearch(offset, limit, orderBy, orderByDir, options);

        return BarMatchApi.get().readList(`/api/match/search?${search.toString()}`, BarMatch.parse);
    }

    public static count(
        offset: number = 0,
        orderBy: string, orderByDir: string,
        options: SearchOptions
    ): Promise<Loading<number>> {
        const search: URLSearchParams = this.buildSearch(offset, 1001, orderBy, orderByDir, options);

        return BarMatchApi.get().readSingle(`/api/match/count?${search.toString()}`, (iter: any) => { return iter as number; });
    }

    private static buildSearch(
        offset: number = 0, limit: number = 24,
        orderBy: string, orderByDir: string,
        options: SearchOptions
    ) : URLSearchParams {
        const search: URLSearchParams = new URLSearchParams();
        search.set("offset", encodeURIComponent(offset));
        search.set("limit", encodeURIComponent(limit));
        search.set("orderBy", encodeURIComponent(orderBy));
        search.set("orderByDir", encodeURIComponent(orderByDir));

        if (options.engine != undefined) {
            search.set("engine", options.engine.trim());
        }
        if (options.gameVersion != undefined) {
            search.set("gameVersion", options.gameVersion.trim());
        }
        if (options.map != undefined) {
            search.set("map", options.map.trim());
        }
        if (options.startTimeAfter != undefined) {
            search.set("startTimeAfter", options.startTimeAfter.toISOString());
        }
        if (options.startTimeBefore != undefined) {
            search.set("startTimeBefore", options.startTimeBefore.toISOString());
        }
        if (options.durationMinimum != undefined) {
            search.set("durationMinimum", options.durationMinimum.toString());
        }
        if (options.durationMaximum != undefined) {
            search.set("durationMaximum", options.durationMaximum.toString());
        }
        if (options.ranked != undefined) {
            search.set("ranked", options.ranked ? "true" : "false");
        }
        if (options.gamemode != undefined) {
            search.set("gamemode", options.gamemode.toString());
        }
        if (options.playerCountMinimum != undefined) {
            search.set("playerCountMinimum", options.playerCountMinimum.toString());
        }
        if (options.playerCountMaximum != undefined) {
            search.set("playerCountMaximum", options.playerCountMaximum.toString());
        }
        if (options.legionEnabled != undefined) {
            search.set("legionEnabled", options.legionEnabled ? "true" : "false");
        }
        if (options.poolID != undefined) {
            search.set("poolID", options.poolID.toString());
        }
        if (options.gameSettings != undefined) {
            for (const gs of options.gameSettings) {
                search.append("gameSettings", `${gs.key},${gs.value},${gs.operation}`);
            }
        }
        if (options.users != undefined) {
            for (const user of options.users) {
                search.append("userIDs", user.userID.toString());
            }
        }
        if (options.players != undefined) {
            for (const player of options.players) {
                search.append("players", JSON.stringify(player));
            }
        }
        if (options.minOS != undefined) {
            search.set("minimumOS", options.minOS.toString());
        }
        if (options.maxOS != undefined) {
            search.set("maximumOS", options.maxOS.toString());
        }
        if (options.minAvgOS != undefined) {
            search.set("minimumAverageOS", options.minAvgOS.toString());
        }
        if (options.maxAvgOS != undefined) {
            search.set("maximumAverageOS", options.maxAvgOS.toString());
        }
        if (options.processingDownloaded != undefined) {
            search.set("processingDownloaded", options.processingDownloaded ? "true" : "false");
        }
        if (options.processingParsed != undefined) {
            search.set("processingParsed", options.processingParsed ? "true" : "false");
        }
        if (options.processingReplayed != undefined) {
            search.set("processingReplayed", options.processingReplayed ? "true" : "false");
        }
        if (options.processingAction != undefined) {
            search.set("processingAction", options.processingAction ? "true" : "false");
        }

        return search;
    }

    public static recalculatePlayerStartSpots(gameID: string): Promise<Loading<void>> {
        return BarMatchApi.get().post(`/api/match/${gameID}/recalculate-player-start-spots`);
    }

}