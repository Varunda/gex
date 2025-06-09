
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { BarLeaderboardSkillEntry } from "model/BarLeaderboardSkillEntry";

export class BarLeaderboardApi extends ApiWrapper<BarLeaderboardSkillEntry> {
    private static _instance: BarLeaderboardApi = new BarLeaderboardApi();
    public static get(): BarLeaderboardApi { return BarLeaderboardApi._instance; }

    public static getSkillLeaderboard(): Promise<Loading<BarLeaderboardSkillEntry[]>> {
        return BarLeaderboardApi.get().readList(`/api/leaderboard/skill`, BarLeaderboardSkillEntry.parse);
    }

}