import { BarMatch } from "model/BarMatch";
import { GameOutput } from "model/GameOutput";

export class CommanderData {
    public unitID: number = 0;
    public positions: { x: number, z: number, frame: number }[] = [];
    public teamID: number = 0;
 
    public static compute(output: GameOutput): CommanderData[] {

        const map: Map<number, CommanderData> = new Map();

        for (const update of output.commanderPositionUpdates) {
            const entry: CommanderData = map.get(update.unitID) ?? {
                unitID: update.unitID,
                positions: [],
                teamID: 0
            };

            entry.positions.push({ x: update.unitX, z: update.unitZ, frame: update.frame });

            map.set(update.unitID, entry);
        }

        let teamIdsLeft: number = map.size;
        for (const created of output.unitsCreated) {
            if (map.has(created.unitID)) {
                map.get(created.unitID)!.teamID = created.teamID;
                --teamIdsLeft;
            }

            if (teamIdsLeft == 0) {
                console.log(`ComputeCommanderData> found all commanders being created!`);
                break;
            }
        }

        return Array.from(map.values());
    }

}
