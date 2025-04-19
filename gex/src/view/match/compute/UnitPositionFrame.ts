import { BarMatch } from "model/BarMatch";
import { GameEventUnitCreated } from "model/GameEventUnitCreated";
import { GameEventUnitKilled } from "model/GameEventUnitKilled";
import { GameEventUnitPosition } from "model/GameEventUnitPosition";
import { GameOutput } from "model/GameOutput";

//type PositionEvents = { action: "unit_created" } & (GameEventUnitCreated | GameEventUnitKilled | GameEventUnitPosition);
type PositionEvents = GameEventUnitCreated | GameEventUnitKilled | GameEventUnitPosition;

type TeamPos = { x: number, y: number, z: number, teamID: number };

export class UnitPositionFrame {

    public frame: number = 0;
    public unitID: number = 0;
    public teamID: number = 0;
    public x: number = 0;
    public y: number = 0;
    public z: number = 0;

    public static compute(match: BarMatch, output: GameOutput): UnitPositionFrame[] {

        const events: PositionEvents[] = [
            // ignore units from team gaia
            ...output.unitsCreated.filter(iter => match.players.find(p => p.teamID == iter.teamID) != undefined),
            ...output.unitsKilled,
            ...output.unitPosition
        ]

        events.sort((a, b) => {
            if (a.frame == b.frame) {
                // unit created comes first
                if (a.action == "unit_created" && b.action != "unit_created") {
                    return -1;
                }

                // then units killed
                if (a.action == "unit_killed" && b.action != "unit_killed") {
                    return -1;
                }

                // finally, make sure all unit positions go last
                if (a.action == "unit_position" && b.action != "unit_position") {
                    return 1;
                }

            }

            return a.frame - b.frame;
        });

        const unitsAlive: Set<number> = new Set();
        let unitsLeft: Set<number> = new Set();

        let previousFrameIsPosition: boolean = true;
        let previousFrame: number = 0;
        let positionsInThisFrame: number = 0;

        const ret: UnitPositionFrame[] = [];

        const previousPositions: Map<number, TeamPos> = new Map();

        for (const ev of events) {
            if (ev.action == "unit_created") {
                previousPositions.set(ev.unitID, { teamID: ev.teamID, x: ev.unitX, y: ev.unitY, z: ev.unitZ });
                unitsAlive.add(ev.unitID);
            }
            
            else if (ev.action == "unit_killed") {
                unitsAlive.delete(ev.unitID);
            }
            
            else if (ev.action == "unit_position") {
                if (previousFrameIsPosition == false) {
                    //console.log(`UnitPositionFrame> starting unit positions for frame ${ev.frame}`);
                    unitsLeft = new Set(unitsAlive);
                } else {
                    unitsLeft.delete(ev.unitID);
                }

                ret.push({...ev});

                previousPositions.set(ev.unitID, ev);
                previousFrame = ev.frame;
                positionsInThisFrame += 1;
            }
            
            // exhaustive check, will error if one of the actions is not handled in the if/else block
            else {
                const exhaustiveCheck: never = ev;
            }

            // ok, all position changes have been recorded, now record the units that stayed in the same place
            if (previousFrameIsPosition == true && ev.action != "unit_position") {
                //console.log(`UnitPositionFrame> outputting remaining units [frame=${ev.frame}] [prevFrame=${previousFrame}] [left=${unitsLeft.size}]`);

                for (const iter of Array.from(unitsLeft.values())) {
                    positionsInThisFrame += 1;
                    const pos: TeamPos | undefined = previousPositions.get(iter);
                    if (pos == undefined) {
                        console.warn(`UnitPositionFrame> missing last known position of unit ${iter}`);
                        continue;
                    }

                    ret.push({
                        frame: previousFrame,
                        unitID: iter,
                        teamID: pos.teamID,
                        x: pos.x,
                        y: pos.y,
                        z: pos.z
                    });
                }

                //console.log(`UnitPositionFrame> updated positions for ${positionsInThisFrame} units, out of ${unitsAlive.size} alive [frame=${previousFrame}]`);
                positionsInThisFrame = 0;

                unitsLeft.clear();
            }

            previousFrameIsPosition = (ev.action == "unit_position");

        }

        return ret;
    }

}