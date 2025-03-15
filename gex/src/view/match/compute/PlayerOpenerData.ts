import { BarMatch } from "model/BarMatch";
import { BarMatchPlayer } from "model/BarMatchPlayer";
import { GameEventUnitDef } from "model/GameEventUnitDef";
import { GameOutput } from "model/GameOutput";

export class PlayerOpener {

    public teamID: number = 0;
    public playerName: string = "";
    public color: string = "";
    public buildings: GameEventUnitDef[] = [];

    public static compute(match: BarMatch, output: GameOutput): PlayerOpener[] {

        const map: Map<number, PlayerOpener> = new Map();

        let eventsLookedAt: number = 0;
        const maxEventsToLookAt: number = match.players.length * 40;
        const maxFrameToLookAt: number = 30 * 90; // 30 fps, 90 seconds

        let playersLeft: number = match.players.length;

        for (const ev of output.unitsCreated) {
            const teamID: number = ev.teamID;

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
                entry.buildings.push(def);
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