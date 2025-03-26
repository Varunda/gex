import { GameEventExtraStatsUpdate } from "./GameEventArmyValueUpdate";
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

    public static parse(elem: any): GameOutput {

        const unitDefs: GameEventUnitDef[] = elem.unitDefinitions.map((iter: any) => GameEventUnitDef.parse(iter));
        const map: Map<number, GameEventUnitDef> = new Map();
        for (const ud of unitDefs) {
            map.set(ud.definitionID, ud);
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
            unitDamage: elem.unitDamage.map((iter: any) => GameEventUnitDamage.parse(iter))
        };
    }

}