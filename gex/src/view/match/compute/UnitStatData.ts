import { BarMatch } from "model/BarMatch";
import { GameEventUnitDef } from "model/GameEventUnitDef";
import { GameOutput } from "model/GameOutput";
import { unitOfTime } from "moment";


export class UnitStats {
    public defID: number = 0;
    public name: string = "";
    public defName: string = "";
    public teamID: number = 0;

    public definition: GameEventUnitDef | undefined = undefined;

    public produced: number = 0;
    public kills: number = 0;
    public lost: number = 0;
    public metalKilled: number = 0;
    public energyKilled: number = 0;
    public buildPowerKilled: number = 0;

    public static compute(output: GameOutput, match: BarMatch): UnitStats[] {
        const map: Map<string, UnitStats> = new Map();

        const lastFrameBeforeKilled: Map<number, number> = new Map();
        for (const ev of output.teamDiedEvents) {
            lastFrameBeforeKilled.set(ev.teamID, ev.frame);
        }

        const getUnitStats = function(defID: number, unitID: number, teamID: number): UnitStats {
            const key: string = `${teamID}-${defID}`;
            let stats: UnitStats | undefined = map.get(key);
            if (stats != undefined) {
                return stats;
            }

            const unitDef: GameEventUnitDef | undefined = output.unitDefinitions.get(defID);
            stats = {
                defID: defID,
                name: unitDef?.name ?? `<missing ${defID}>`,
                defName: unitDef?.definitionName ?? `missing_${defID}`,
                teamID: teamID,
                definition: unitDef,

                produced: 0,
                kills: 0,
                lost: 0,

                metalKilled: 0,
                energyKilled: 0,
                buildPowerKilled: 0
            };

            map.set(key, stats);

            return stats;
        }

        for (const ev of output.unitsCreated) {
            const stats: UnitStats = getUnitStats(ev.definitionID, 0, ev.teamID);
            stats.produced += 1;
        }

        for (const ev of output.unitsKilled) {
            const lastFrame: number | undefined = lastFrameBeforeKilled.get(ev.teamID);
            if (lastFrame != undefined && ev.frame > lastFrame) {
                continue;
            }

            const stats: UnitStats = getUnitStats(ev.definitionID, 0, ev.teamID);
            stats.lost += 1;

            if (ev.attackerID != null && ev.attackerDefinitionID != null && ev.attackerTeam != null) {
                const attacker: UnitStats = getUnitStats(ev.attackerDefinitionID, 0, ev.attackerTeam);
                attacker.kills += 1;

                const unitDef: GameEventUnitDef | undefined = output.unitDefinitions.get(ev.definitionID);
                if (unitDef != undefined) {
                    attacker.metalKilled += unitDef.metalCost;
                    attacker.energyKilled += unitDef.energyCost;
                    attacker.buildPowerKilled += unitDef.buildPower;
                } else {
                    console.log(`UnitStatData> missing unit def ${ev.definitionID}!`);
                }
            }
        }

        return Array.from(map.values());
    }

}