import { Lut } from "util/Lut";

export class BarMatchPlayer {
    public gameID: string = "";
    public playerID: number = 0;
    public userID: number = 0;
    public username: string = "";
    public teamID: number = 0;
    public allyTeamID: number = 0;
    public skill: number = 0;
    public skillUncertainty: number = 0;

    public static parse(elem: any): BarMatchPlayer {
        return {
            ...elem,
            username: elem.name,
        };
    }
}
