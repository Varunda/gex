
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { BarLeaderboardSkillEntry } from "model/BarLeaderboardSkillEntry";
import { BarSeason } from "model/BarSeason";

export class BarLeaderboardApi extends ApiWrapper<BarLeaderboardSkillEntry> {
    private static _instance: BarLeaderboardApi = new BarLeaderboardApi();
    public static get(): BarLeaderboardApi { return BarLeaderboardApi._instance; }

    public static getSkillLeaderboard(count: number = 10, season: number = -1): Promise<Loading<BarLeaderboardSkillEntry[]>> {
        return BarLeaderboardApi.get().readList(`/api/leaderboard/skill?season=${season}&count=${count}`, BarLeaderboardSkillEntry.parse);
    }

    public static getSeasons(): Promise<Loading<BarSeason[]>> {
        return BarLeaderboardApi.get().readList(`/api/leaderboard/seasons`, BarSeason.parse);
    }

}