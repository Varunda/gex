import { BarMatch } from "model/BarMatch";
import { GameEventUnitDef } from "model/GameEventUnitDef";
import { GameOutput } from "model/GameOutput";
import { BarMatchPlayer } from "model/BarMatchPlayer";

export class FactoryDataUnit {
    public defID: number = 0;
    public name: string = "";
    public count: number = 0;
}

export class FactoryData {
    public factoryID: number = 0;
    public factoryDefinitionID: number = 0;
    public factoryDefinitionName: string = "";
    public name: string = "";
    public teamID: number = 0;
    public position: { x: number, z: number, rotation: number } = { x: 0, z: 0, rotation: 0 };
    public size: { x: number, z: number}  = { x: 0, z: 0 };
    public units: FactoryDataUnit[] = [];
    public totalMade: number = 0;

    public createdAt: number = 0;
    public destroyedAt: number | null = null;
}

export class PlayerFactories {
    public name: string = "";
    public teamID: number = 0;
    public color: string = "";
    public colorInt: number = 0;
    public factories: FactoryData[] = [];

    public static compute(match: BarMatch, output: GameOutput): PlayerFactories[] {

        const map: Map<number, FactoryData> = new Map();

        for (const created of output.unitsCreated) {
            const unitDef: GameEventUnitDef | undefined = output.unitDefinitions.get(created.definitionID);
            if (unitDef == undefined) {
                console.warn(`FactoryData> missing unit def ${created.definitionID}`);
                continue;
            }

            if (unitDef.isFactory == false) {
                continue;
            }

            map.set(created.unitID, {
                factoryID: created.unitID,
                factoryDefinitionID: created.definitionID,
                factoryDefinitionName: unitDef.definitionName,
                name: unitDef.name,
                teamID: created.teamID,
                position: { x: created.unitX, z: created.unitZ, rotation: created.rotation },
                size: { x: unitDef.sizeX, z: unitDef.sizeZ },
                units: [],
                totalMade: 0,
                createdAt: created.frame,
                destroyedAt: null
            });
        }

        for (const killed of output.unitsKilled) {
            if (map.has(killed.unitID) == false) {
                continue;
            }

            map.get(killed.unitID)!.destroyedAt = killed.frame;
        }

        for (const fac of output.factoryUnitCreated) {
            const unitDef: GameEventUnitDef | undefined = output.unitDefinitions.get(fac.definitionID);
            if (unitDef == undefined) {
                console.warn(`FactoryData> missing unit def ${fac.definitionID}`);
                continue;
            }

            let entry: FactoryData | undefined = map.get(fac.factoryUnitID);
            if (entry == undefined) {
                console.warn(`FactoryData> missing factory ${fac.factoryUnitID}, which created unit ${fac.unitID}`);
                continue;
            }

            let unit: FactoryDataUnit | undefined = entry.units.find(iter => iter.defID == fac.definitionID);
            
            if (unit == undefined) {
                unit = {
                    defID: fac.definitionID,
                    name: unitDef.name,
                    count: 0
                };
                entry.units.push(unit);
            }

            unit.count += 1;
            entry.totalMade += 1;
        }

        const facs: FactoryData[] = Array.from(map.values());

        let pf: PlayerFactories[] = [];
        for (const player of match.players) {
            const iter: PlayerFactories = new PlayerFactories();
            iter.teamID = player.teamID;
            iter.name = player.username;
            iter.color = player.hexColor;
            iter.colorInt = player.color;
            iter.factories = facs.filter(iter => iter.teamID == player.teamID);

            pf.push(iter);
        }

        return pf;
    }

}