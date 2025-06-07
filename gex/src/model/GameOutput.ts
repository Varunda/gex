import { GameEventExtraStatsUpdate } from "./GameEventExtraStatsUpdate";
import { GameEventCommanderPositionUpdate } from "./GameEventCommanderPositionUpdate";
import { GameEventFactoryUnitCreated } from "./GameEventFactoryUnitCreated";
import { GameEventTeamDied } from "./GameEventTeamDied";
import { GameEventTeamsStats } from "./GameEventTeamStats";
import { GameEventUnitCreated } from "./GameEventUnitCreated";
import { GameEventUnitDamage } from "./GameEventUnitDamage";
import { GameEventUnitDef } from "./GameEventUnitDef";
import { GameEventUnitKilled } from "./GameEventUnitKilled";
import { GameEventUnitResources } from "./GameEventUnitResources";
import { GameEventWindUpdate } from "./GameEventWindUpdate";
import { GameEventUnitPosition } from "./GameEventUnitPosition";

export class GameOutput {

    public gameID: string = "";
    public unitDefinitions: Map<number, GameEventUnitDef> = new Map(); //[] = [];
    public windUpdates: GameEventWindUpdate[] = [];
    public unitsCreated: GameEventUnitCreated[] = [];
    public unitsKilled: GameEventUnitKilled[] = [];
    public factoryUnitCreated: GameEventFactoryUnitCreated[] = [];
    public commanderPositionUpdates: GameEventCommanderPositionUpdate[] = [];
    public extraStats: GameEventExtraStatsUpdate[] = [];
    public teamStats: GameEventTeamsStats[] = [];
    public teamDiedEvents: GameEventTeamDied[] = [];
    public unitResources: GameEventUnitResources[] = [];
    public unitDamage: GameEventUnitDamage[] = [];
    public unitPosition: GameEventUnitPosition[] = [];

    public static parse(elem: any): GameOutput {

        const unitDefs: GameEventUnitDef[] = elem.unitDefinitions.map((iter: any) => GameEventUnitDef.parse(iter));
        const map: Map<number, GameEventUnitDef> = new Map();
        for (const ud of unitDefs) {
            map.set(ud.definitionID, ud);
        }

        const mapName: Map<string, number[]> = new Map();
        for (const ud of unitDefs) {
            let name: number[] = mapName.get(ud.name) ?? [];
            name.push(ud.definitionID);

            mapName.set(ud.name, name);
        }

        for (const kvp of Array.from(mapName.entries())) {
            const key: string = kvp[0];
            const value: number[] = kvp[1];

            if (value.length == 1) {
                continue;
            }

            console.log(`GameOutput> clashing name found '${key}' between [${value}]`);

            for (const defID of value) {
                const entry: GameEventUnitDef | undefined = map.get(defID);
                if (entry == undefined) {
                    console.error(`GameOutput> missing unit definition ${defID}, but it clashes with name '${key}'?`);
                    continue;
                }

                if (entry.definitionName.startsWith("arm")) {
                    entry.disambiguatedName += " (Armada)";
                } else if (entry.definitionName.startsWith("cor")) {
                    entry.disambiguatedName += " (Cortex)";
                } else if (entry.definitionName.startsWith("leg")) {
                    entry.disambiguatedName += " (Legion)";
                } else if (entry.definitionName.startsWith("lootbox")
                    || entry.definitionName.startsWith("critter")
                    || entry.definitionName.startsWith("dbg")
                    || entry.definitionName.startsWith("xmasball")) {

                    continue;
                } else {
                    console.warn(`GameOutput> unchecked definition name '${entry.definitionName}' does not start with arm|cor|leg`);
                }
            }
        }

        return {
            gameID: elem.gameID,
            unitDefinitions: map,
            windUpdates: elem.windUpdates.map((iter: any) => GameEventWindUpdate.parse(iter)),
            unitsCreated: elem.unitsCreated.map((iter: any) => GameEventUnitCreated.parse(iter)),
            unitsKilled: elem.unitsKilled.map((iter: any) => GameEventUnitKilled.parse(iter)),
            factoryUnitCreated: elem.factoryUnitCreated.map((iter: any) => GameEventFactoryUnitCreated.parse(iter)),
            commanderPositionUpdates: elem.commanderPositionUpdates.map((iter: any) => GameEventCommanderPositionUpdate.parse(iter)),
            extraStats: elem.extraStats.map((iter: any) => GameEventExtraStatsUpdate.parse(iter)),
            teamStats: elem.teamStats.map((iter: any) => GameEventTeamsStats.parse(iter)),
            teamDiedEvents: elem.teamDiedEvents.map((iter: any) => GameEventTeamDied.parse(iter)),
            unitResources: elem.unitResources.map((iter: any) => GameEventUnitResources.parse(iter)),
            unitDamage: elem.unitDamage.map((iter: any) => GameEventUnitDamage.parse(iter)),
            unitPosition: elem.unitPosition.map((iter: any) => GameEventUnitPosition.parse(iter))
        };
    }

}