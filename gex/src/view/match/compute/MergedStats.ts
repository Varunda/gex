import { GameEventExtraStatsUpdate } from "model/GameEventExtraStatsUpdate";
import { GameOutput } from "model/GameOutput";

export default class MergedStats {

    public gameID: string = "";
    public frame: number = 0;
    public teamID: number = 0;
    public metalProduced: number = 0;
    public metalUsed: number = 0;
    public metalExcess: number = 0;
    public metalSent: number = 0;
    public metalReceived: number = 0;
    public energyProduced: number = 0;
    public energyUsed: number = 0;
    public energyExcess: number = 0;
    public energySent: number = 0;
    public energyReceived: number = 0;
    public damageDealt: number = 0;
    public damageReceived: number = 0;
    public unitsReceived: number = 0;
    public unitsKilled: number = 0;
    public unitsProduced: number = 0;
    public unitsSent: number = 0;
    public unitsCaptured: number = 0;
    public unitsOutCaptured: number = 0;
    public totalValue: number = 0;
    public armyValue: number = 0;
    public defenseValue: number = 0;
    public utilValue: number = 0;
    public ecoValue: number = 0;
    public otherValue: number = 0;
    public buildPowerAvailable: number = 0;
    public buildPowerUsed: number = 0;
    public buildPowerPercent: number = 0;

    public static compute(output: GameOutput): MergedStats[] {

        const map: Map<string, GameEventExtraStatsUpdate> = new Map();

        for (const ev of output.extraStats) {
            const key = `${ev.teamID}-${ev.frame}`;
            if (map.has(key)) {
                console.warn(`MergedStats> duplicate stat found [key=${key}] [teamID=${ev.teamID}] [frame=${ev.frame}]`);
            }

            map.set(key, ev);
        }

        const last: Map<number, GameEventExtraStatsUpdate> = new Map();

        return output.teamStats.map((iter) => {
            const key = `${iter.teamID}-${iter.frame}`;
            let extra: GameEventExtraStatsUpdate | undefined = map.get(key);
            if (extra == undefined) {
                console.log(`MergedStats> trying previous frame for stats [key=${key}] [frame=${iter.frame}] [team=${iter.teamID}]`);
                extra = map.get(`${iter.teamID}-${iter.frame - 1}`);
            }

            if (extra == undefined) {
                extra = last.get(iter.teamID);
                console.warn(`MergedStats> missing extra stats [key=${key}] [frame=${iter.frame}] [teamID=${iter.teamID}] [last?=${!!extra}]`);
            } else {
                last.set(iter.teamID, extra);
            }

            return {
                ...iter,
                totalValue: extra?.totalValue ?? 0,
                armyValue: extra?.armyValue ?? 0,
                ecoValue: extra?.ecoValue ?? 0,
                defenseValue: extra?.defenseValue ?? 0,
                utilValue: extra?.utilValue ?? 0,
                otherValue: extra?.otherValue ?? 0,
                buildPowerAvailable: extra?.buildPowerAvailable ?? 0,
                buildPowerUsed: extra?.buildPowerUsed ?? 0,
                buildPowerPercent: (extra?.buildPowerUsed ?? 0) / Math.max(1, (extra?.buildPowerAvailable ?? 0)) * 100
            };
        });
    }


}