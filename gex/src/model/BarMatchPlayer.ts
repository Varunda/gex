import { Lut } from "util/Lut";

export class BarMatchPlayer {
    public gameID: string = "";
    public playerID: number = 0;
    public userID: number = 0;
    public username: string = "";
    public teamID: number = 0;
    public allyTeamID: number = 0;
    public faction: string = "";
    public startingPosition: { x: number, y: number, z: number } = { x: 0, y: 0, z: 0 };
    public skill: number = 0;
    public skillUncertainty: number = 0;
    public color: number = 0;
    public hexColor: string = "";
    public handicap: number = 0;

    public static parse(elem: any): BarMatchPlayer {
        const lut: number = Lut.lut(elem.color);

        return {
            ...elem,
            color: lut,
            username: elem.name,
            hexColor: "#" + lut.toString(16).padStart(6, "0")
        };
    }
}
