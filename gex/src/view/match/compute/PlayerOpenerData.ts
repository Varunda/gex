import { BarMatch } from "model/BarMatch";
import { BarMatchPlayer } from "model/BarMatchPlayer";
import { GameEventUnitDef } from "model/GameEventUnitDef";
import { GameOutput } from "model/GameOutput";

export class OpenerEntry {
    public defName: string = "";
    public name: string = "";
    public amount: number = 0;
    public isFactory: boolean = false;
    public firstFrame: number = 0;
}

export class PlayerOpener {

    public teamID: number = 0;
    public playerName: string = "";
    public color: string = "";
    public buildings: OpenerEntry[] = [];
    public units: OpenerEntry[] = [];
    public playerFaction: string = "";

    public static compute(match: BarMatch, output: GameOutput): PlayerOpener[] {

        const map: Map<number, PlayerOpener> = new Map();

        let eventsLookedAt: number = 0;
        const maxEventsToLookAt: number = match.players.length * 40;
        const maxFrameToLookAt: number = 30 * 90; // 30 fps, 90 seconds
        const maxFrameUnitsToLookAt: number = 30 * 60 * 3; // 3 minutes for units (not buildings)

        let playersLeft: number = match.players.length;

        for (const ev of output.unitsCreated) {
            const teamID: number = ev.teamID;

            if (match.players.find(iter => iter.teamID == teamID) == undefined) {
                continue;
            }

            const entry: PlayerOpener = map.get(teamID) ?? {
                teamID: teamID,
                buildings: [],
                units: [],
                playerName: "",
                color: "",
                playerFaction: ""
            };

            const def: GameEventUnitDef | undefined = output.unitDefinitions.get(ev.definitionID);
            if (def == undefined) {
                console.warn(`PlayerOpenerData> missing unit def [unitDefID=${ev.definitionID}]`);
                continue;
            }

            if (def.speed == 0 && ev.frame < maxFrameToLookAt) {
                if (entry.buildings.length > 0 && entry.buildings[entry.buildings.length - 1].defName == def.definitionName) {
                    entry.buildings[entry.buildings.length - 1].amount += 1;
                } else {
                    entry.buildings.push({
                        defName: def.definitionName,
                        name: def.name,
                        amount: 1,
                        isFactory: def.isFactory,
                        firstFrame: ev.frame
                    });
                }
            } else if (def.speed > 0 && def.isCommander == false) {
                if (entry.units.length > 0 && entry.units[entry.units.length - 1].defName == def.definitionName) {
                    entry.units[entry.units.length - 1].amount += 1;
                } else {
                    entry.units.push({
                        defName: def.definitionName,
                        name: def.name,
                        amount: 1,
                        isFactory: false,
                        firstFrame: ev.frame
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

            if (ev.frame >= maxFrameUnitsToLookAt) {
                console.log(`PlayerOpenerData> exiting after ${maxFrameToLookAt} frames, this is probably not the opener anymore`);
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
            opener.playerFaction = p?.faction ?? `<unknown>`;
        });

        return Array.from(map.values());
    }

}