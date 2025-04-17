

export class GamemodeUtil {

    public static UNKNOWN: number = 0;

    public static DUEL: number = 1;

    public static SMALL_TEAM: number = 2;

    public static LARGE_TEAM: number = 3;

    public static FFA: number = 4;

    public static TEAM_FFA: number = 5;

    public static getName(gamemode: number): string {
        if (gamemode == GamemodeUtil.DUEL) {
            return "Duel";
        } else if (gamemode == GamemodeUtil.SMALL_TEAM) {
            return "Small team";
        } else if (gamemode == GamemodeUtil.LARGE_TEAM) {
            return "Large team";
        } else if (gamemode == GamemodeUtil.FFA) {
            return "FFA";
        } else if (gamemode == GamemodeUtil.TEAM_FFA) {
            return "Team FFA";
        }

        return `Unchecked ${gamemode}`;
    }


}