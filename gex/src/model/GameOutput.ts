import { GameEventArmyValueUpdate } from "./GameEventArmyValueUpdate";
import { GameEventCommanderPositionUpdate } from "./GameEventCommanderPositionUpdate";
import { GameEventFactoryUnitCreated } from "./GameEventFactoryUnitCreated";
import { GameEventTeamsStats } from "./GameEventTeamStats";
import { GameEventUnitCreated } from "./GameEventUnitCreated";
import { GameEventUnitDef } from "./GameEventUnitDef";
import { GameEventUnitKilled } from "./GameEventUnitKilled";
import { GameEventWindUpdate } from "./GameEventWindUpdate";


export class GameOutput {

    public gameID: string = "";
    public unitDefinitions: GameEventUnitDef[] = [];
    public windUpdates: GameEventWindUpdate[] = [];
    public unitsCreated: GameEventUnitCreated[] = [];
    public unitsKilled: GameEventUnitKilled[] = [];
    public factoryUnitCreated: GameEventFactoryUnitCreated[] = [];
    public commanderPositionUpdates: GameEventCommanderPositionUpdate[] = [];
    public armyValueUpdates: GameEventArmyValueUpdate[] = [];
    public teamStats: GameEventTeamsStats[] = [];

    public static parse(elem: any): GameOutput {
        return {
            gameID: elem.gameID,
            unitDefinitions: elem.unitDefinitions.map((iter: any) => GameEventUnitDef.parse(iter)),
            windUpdates: elem.windUpdates.map((iter: any) => GameEventWindUpdate.parse(iter)),
            unitsCreated: elem.unitsCreated.map((iter: any) => GameEventUnitCreated.parse(iter)),
            unitsKilled: elem.unitsKilled.map((iter: any) => GameEventUnitKilled.parse(iter)),
            factoryUnitCreated: elem.factoryUnitCreated.map((iter: any) => GameEventFactoryUnitCreated.parse(iter)),
            commanderPositionUpdates: elem.commanderPositionUpdates.map((iter: any) => GameEventCommanderPositionUpdate.parse(iter)),
            armyValueUpdates: elem.armyValueUpdates.map((iter: any) => GameEventArmyValueUpdate.parse(iter)),
            teamStats: elem.teamStats.map((iter: any) => GameEventTeamsStats.parse(iter))
        };
    }

}