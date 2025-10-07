import { BarMatch } from "model/BarMatch";
import { GameEventUnitDef } from "model/GameEventUnitDef";
import { GameEventUnitKilled } from "model/GameEventUnitKilled";
import { GameOutput } from "model/GameOutput";

export class UnitStats {
    public id: string = "";
    public defID: number = 0;
    public name: string = "";
    public defName: string = "";

    public definition: GameEventUnitDef | undefined = undefined;

    public produced: number = 0;
    public rank: number = 0;
    public kills: number = 0;
    public mobileKills: number = 0;
    public staticKills: number = 0;
    public lost: number = 0;
    public reclaimed: number = 0;
    public reclaims: number = 0;

    public damageDealt: number = 0;
    public damageTaken: number = 0;

    public metalKilled: number = 0;
    public energyKilled: number = 0;
    public buildPowerKilled: number = 0;

    public damageRatio: number = 0;
    public metalRatio: number = 0;
    public energyRatio: number = 0;

    public static compute(output: GameOutput, match: BarMatch): UnitStats[] {
        const map: Map<string, UnitStats> = new Map();

        const lastFrameBeforeKilled: Map<number, number> = new Map();
        for (const ev of output.teamDiedEvents) {
            lastFrameBeforeKilled.set(ev.teamID, ev.frame);
        }

        const allyTeamMapping: Map<number, number> = new Map();
        for (const player of match.players) {
            allyTeamMapping.set(player.teamID, player.allyTeamID);
        }

        const getUnitStats = function(type: string, defID: number): UnitStats {
            const key: string = `${type}-${defID}`;
            let stats: UnitStats | undefined = map.get(key);
            if (stats != undefined) {
                return stats;
            }

            const unitDef: GameEventUnitDef | undefined = output.unitDefinitions.get(defID);
            stats = {
                defID: defID,
                name: unitDef?.name ?? `<missing ${defID}>`,
                defName: unitDef?.definitionName ?? `missing_${defID}`,
                id: type,
                definition: unitDef,

                produced: 0,
                rank: 0,
                kills: 0,
                mobileKills: 0,
                staticKills: 0,
                lost: 0,
                reclaimed: 0,
                reclaims: 0,

                damageDealt: 0,
                damageTaken: 0,

                metalKilled: 0,
                energyKilled: 0,
                buildPowerKilled: 0,

                damageRatio: 0,
                metalRatio: 0,
                energyRatio: 0
            };

            map.set(key, stats);

            return stats;
        }

        const updateAttackerStats = (type: string, ev: GameEventUnitKilled): void => {
            if (ev.attackerID != null && ev.attackerDefinitionID != null && ev.attackerTeam != null) {
                const attacker: UnitStats = getUnitStats(type, ev.attackerDefinitionID);

                if (ev.weaponDefinitionID == -12) {
                    attacker.reclaims += 1;
                } else {
                    attacker.kills += 1;

                    const unitDef: GameEventUnitDef | undefined = output.unitDefinitions.get(ev.definitionID);
                    if (unitDef != undefined) {
                        attacker.metalKilled += unitDef.metalCost;
                        attacker.energyKilled += unitDef.energyCost;
                        attacker.buildPowerKilled += unitDef.buildPower;

                        if (unitDef.speed == 0) {
                            attacker.staticKills += 1;
                        } else if (unitDef.speed > 0) {
                            attacker.mobileKills += 1;
                        } else {
                            console.log(`UnitStatData> unitDef is not static mobile ${JSON.stringify(unitDef)}`);
                        }
                    } else {
                        console.log(`UnitStatData> missing unit def ${ev.definitionID}!`);
                    }
                }
            }
        };

        for (const ev of output.unitsCreated) {
            const stats: UnitStats = getUnitStats(`team-${ev.teamID}`, ev.definitionID);
            stats.produced += 1;

            const stats2: UnitStats = getUnitStats(`ally-team-${allyTeamMapping.get(ev.teamID)}`, ev.definitionID);
            stats2.produced += 1;
        }

        for (const ev of output.unitsKilled) {
            const lastFrame: number | undefined = lastFrameBeforeKilled.get(ev.teamID);
            if (lastFrame != undefined && ev.frame > lastFrame) {
                continue;
            }

            const stats: UnitStats = getUnitStats(`team-${ev.teamID}`, ev.definitionID);
            if (ev.teamID == ev.attackerTeam && ev.weaponDefinitionID == -12) {
                stats.reclaimed += 1;
            } else {
                stats.lost += 1;
            }

            const stats2: UnitStats = getUnitStats(`ally-team-${allyTeamMapping.get(ev.teamID)}`, ev.definitionID);
            if (ev.teamID == ev.attackerTeam && ev.weaponDefinitionID == -12) {
                stats2.reclaimed += 1;
            } else {
                stats2.lost += 1;
            }

            if (ev.attackerID != null && ev.attackerDefinitionID != null && ev.attackerTeam != null) {
                updateAttackerStats(`team-${ev.attackerTeam}`, ev);
                updateAttackerStats(`ally-team-${allyTeamMapping.get(ev.attackerTeam)}`, ev);
            }
        }

        for (const ev of output.unitsGiven) {
            const stats: UnitStats = getUnitStats(`team-${ev.newTeamID}`, ev.definitionID);
            stats.produced += 1;

            const stats2: UnitStats = getUnitStats(`ally-team-${allyTeamMapping.get(ev.newTeamID)}`, ev.definitionID);
            stats2.produced += 1;
        }

        for (const ev of output.unitDamage) {
            const lastFrame: number | undefined = lastFrameBeforeKilled.get(ev.teamID);
            if (lastFrame != undefined && ev.frame > lastFrame) {
                continue;
            }

            const stats: UnitStats = getUnitStats(`team-${ev.teamID}`, ev.definitionID);
            stats.damageDealt += ev.damageDealt;
            stats.damageTaken += ev.damageTaken;

            const stats2: UnitStats = getUnitStats(`ally-team-${allyTeamMapping.get(ev.teamID)}`, ev.definitionID);
            stats2.damageDealt += ev.damageDealt;
            stats2.damageTaken += ev.damageTaken;
        }

        const arr: UnitStats[] = Array.from(map.values());

        for (const elem of arr) {
            if (elem.definition?.isCommander) {
                elem.rank = 99999;
            } else {
                elem.rank = elem.produced;
            }

            elem.damageRatio = elem.damageDealt / Math.max(1, elem.damageTaken);
            elem.metalRatio = elem.metalKilled / Math.max(1, (elem.produced * (elem.definition?.metalCost ?? 1)));
            elem.energyRatio = elem.energyKilled / Math.max(1, (elem.produced * (elem.definition?.energyCost ?? 1)));
        }

        return arr;
    }

}