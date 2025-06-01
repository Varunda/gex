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

    public static getByUserID(userID: number): Promise<Loading<BarMatch[]>> {
        return BarMatchApi.get().readList(`/api/match/user/${userID}`, BarMatch.parse);
    }

    public static search(
        offset: number = 0, limit: number = 24,
        options: { 
            engine?: string, gameVersion?: string, map?: string, startTimeAfter?: Date, startTimeBefore?: Date,
            durationMinimum?: number, durationMaximum?: number, ranked?: boolean, gamemode?: number,
            playerCountMinimum?: number, playerCountMaximum?: number,
            processingDownloaded?: boolean, processingParsed?: boolean, processingReplayed?: boolean, processingAction?: boolean
        }
    ) {

        const search: URLSearchParams = new URLSearchParams();
        search.set("offset", encodeURIComponent(offset));
        search.set("limit", encodeURIComponent(limit));

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

        return BarMatchApi.get().readList(`/api/match/search?${search.toString()}`, BarMatch.parse);
    }

}