import { BarMatch } from "model/BarMatch";
import { BarMatchPlayer } from "model/BarMatchPlayer";
import { GameEventUnitDef } from "model/GameEventUnitDef";
import { GameOutput } from "model/GameOutput";

export class OpenerEntry {
    public defName: string = "";
    public name: string = "";
    public amount: number = 0;
    public isFactory: boolean = false;
}

export class PlayerOpener {

    public teamID: number = 0;
    public playerName: string = "";
    public color: string = "";
    public buildings: OpenerEntry[] = [];

    public static compute(match: BarMatch, output: GameOutput): PlayerOpener[] {

        const map: Map<number, PlayerOpener> = new Map();

        let eventsLookedAt: number = 0;
        const maxEventsToLookAt: number = match.players.length * 40;
        const maxFrameToLookAt: number = 30 * 90; // 30 fps, 90 seconds

        let playersLeft: number = match.players.length;

        for (const ev of output.unitsCreated) {
            const teamID: number = ev.teamID;

            if (match.players.find(iter => iter.teamID == teamID) == undefined) {
                continue;
            }

            const entry: PlayerOpener = map.get(teamID) ?? {
                teamID: teamID,
                buildings: [],
                playerName: "",
                color: ""
            };

            const def: GameEventUnitDef | undefined = output.unitDefinitions.get(ev.definitionID);
            if (def == undefined) {
                console.warn(`PlayerOpenerData> missing unit def [unitDefID=${ev.definitionID}]`);
                continue;
            }

            if (def.speed == 0) {

                if (entry.buildings.length > 0 && entry.buildings[entry.buildings.length - 1].defName == def.definitionName) {
                    entry.buildings[entry.buildings.length - 1].amount += 1;
                } else {
                    entry.buildings.push({
                        defName: def.definitionName,
                        name: def.name,
                        amount: 1,
                        isFactory: def.isFactory
                    });
                }
            }

            if (map.has(teamID) == false) {
                console.log(`PlayerOpenerData> new team ${teamID} from ${JSON.stringify(ev)}`);
            }

            map.set(teamID, entry);

            if (entry.buildings.length > 8) {
                --playersLeft;
                if (playersLeft == 0) {
                    break;
                }
                continue;
            }

            if (ev.frame >= (maxFrameToLookAt)) {
                console.log(`PlayerOpenerData> existing after ${maxFrameToLookAt} frames, this is probably not the opener anymore`);
                break;
            }

            if (++eventsLookedAt >= maxEventsToLookAt) {
                console.log(`PlayerOpenerData> exiting player opener after ${maxEventsToLookAt} events (of ${output.unitsCreated.length})`);
                break;
            }
        }

        map.forEach((opener: PlayerOpener, teamID: number) => {
            const p: BarMatchPlayer | undefined = match.players.find(iter => iter.teamID == teamID);
            opener.playerName = p?.username ?? `<missing team ${teamID}>`;
            opener.color = p?.hexColor ?? "#000000";
        });

        return Array.from(map.values());
    }

}