import { BarMatch } from "model/BarMatch";
import { GameEventUnitDef } from "model/GameEventUnitDef";
import { GameOutput } from "model/GameOutput";

export class ResourceProductionEntry {
    public definitionID: number = 0;
    public defName: string = "";
    public name: string = "";
    public count: number = 0;
    public rank: number = 0;
    public lost: number = 0;
    public reclaimed: number = 0;

    public definition: GameEventUnitDef | undefined = undefined;

    public metalMade: number = 0;
    public metalUsed: number = 0;
    public energyMade: number = 0;
    public energyUsed: number = 0;
}

export class ResourceProductionData {
    public teamID: number = 0;
    public units: ResourceProductionEntry[] = [];
    public color: string = "";
    public username: string = "";

    public static compute(match: BarMatch, output: GameOutput): ResourceProductionData[] {

        const lastFrameBeforeKilled: Map<number, number> = new Map();
        for (const ev of output.teamDiedEvents) {
            lastFrameBeforeKilled.set(ev.teamID, ev.frame);
        }

        const map: Map<number, ResourceProductionData> = new Map();

        for (const ev of output.unitResources) {

            const entry: ResourceProductionData = map.get(ev.teamID) ?? {
                teamID: ev.teamID,
                units: [],
                color: "",
                username: ""
            };

            let unit: ResourceProductionEntry | undefined = entry.units.find(iter => iter.definitionID == ev.definitionID)
            if (unit == undefined) {
                const def: GameEventUnitDef | undefined = output.unitDefinitions.get(ev.definitionID);
                if (def == undefined) {
                    console.warn(`ResourceProductionData> missing unit def ${ev.definitionID}!`);
                    continue;
                }

                unit = {
                    definitionID: ev.definitionID,
                    name: def.name,
                    defName: def.definitionName,
                    definition: def,
                    count: 0,
                    rank: 0,
                    lost: 0,
                    reclaimed: 0,
                    metalMade: 0,
                    metalUsed: 0,
                    energyMade: 0,
                    energyUsed: 0
                };

                entry.units.push(unit);
            }

            unit.count += 1;
            unit.metalMade += ev.metalMade;
            unit.metalUsed += ev.metalUsed;
            unit.energyMade += ev.energyMade;
            unit.energyUsed += ev.energyUsed;

            map.set(ev.teamID, entry);
        }

        for (const ev of output.unitsKilled) {
            const lastFrame: number | undefined = lastFrameBeforeKilled.get(ev.teamID);
            if (lastFrame != undefined && ev.frame > lastFrame) {
                continue;
            }

            const entry: ResourceProductionData | undefined = map.get(ev.teamID);
            if (entry == undefined) {
                continue;
            }

            const unit: ResourceProductionEntry | undefined = entry.units.find(iter => iter.definitionID == ev.definitionID);
            if (unit == undefined) {
                continue;
            }

            // -12 is reclaim
            // https://github.com/beyond-all-reason/RecoilEngine/blob/cdfc9d7b872c3d890fc7c77bcb645a23cd9ec8e5/rts/Sim/Objects/SolidObject.h#L93-L123
            if (ev.teamID == ev.attackerTeam && ev.weaponDefinitionID == -12) {
                unit.reclaimed += 1;
            } else {
                unit.lost += 1;
            }
        }

        const ret: ResourceProductionData[] = Array.from(map.values());
        for (const team of ret) {
            team.color = match.players.find(iter => team.teamID == iter.teamID)?.hexColor ?? "#333333";
            team.username = match.players.find(iter => iter.teamID == team.teamID)?.username ?? `<missing ${team.teamID}>`;

            for (const entry of team.units) {
                entry.rank = entry.count;
                if (entry.definition && entry.definition.isCommander) {
                    entry.rank = Number.MAX_VALUE;
                }
            }
        }

        return ret;
    }
}