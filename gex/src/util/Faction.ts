
export class FactionUtil {

    public static UNKNOWN: number = 0;

    public static ARMADA: number = 1;

    public static CORTEX: number = 2;

    public static LEGION: number = 3;

    public static RANDOM: number = 4;

    public static getValue(faction: string): number {
        faction = faction.toLowerCase();

        if (faction.startsWith("arm")) {
            return FactionUtil.ARMADA;
        } else if (faction.startsWith("cor")) {
            return FactionUtil.CORTEX;
        } else if (faction.startsWith("leg")) {
            return FactionUtil.LEGION;
        } else if (faction == "random") {
            return FactionUtil.RANDOM;
        }

        return FactionUtil.UNKNOWN;
    }

    public static getName(gamemode: number): string {
        if (gamemode == FactionUtil.ARMADA) {
            return "Armada";
        } else if (gamemode == FactionUtil.CORTEX) {
            return "Cortex";
        } else if (gamemode == FactionUtil.LEGION) {
            return "Legion";
        } else if (gamemode == FactionUtil.RANDOM) {
            return "Random";
        }

        return `Unchecked ${gamemode}`;
    }


}