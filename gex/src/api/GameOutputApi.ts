
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { GameOutput } from "model/GameOutput";

export class GameOutputApi extends ApiWrapper<GameOutput> {
    private static _instance: GameOutputApi = new GameOutputApi();
    public static get(): GameOutputApi { return GameOutputApi._instance; }

    public static getEvents(gameID: string): Promise<Loading<GameOutput>> {
        return GameOutputApi.get().readSingle(`/api/game-event/${gameID}?includeTeamStats=true&includeUnitsKilled=true`
            + `&includeUnitsCreated=true&includeUnitDefs=true&includeExtraStats=true&includeWindUpdates=true`
            + `&includeCommanderPositionUpdates=true&includeFactoryUnitCreate=true&includeUnitsGiven=true&includeUnitsTaken=true`
            + `&includeTransportLoads=true&includeTransportUnloads=true&includeTeamDiedEvents=true&includeUnitResources=true`
            + `&includeUnitDamage=true`, GameOutput.parse);
    }


}