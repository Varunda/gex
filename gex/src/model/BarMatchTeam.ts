import { Lut } from "util/Lut";

export class BarMatchTeam {
    public gameID: string = "";
    public teamID: number = 0;
    public allyTeamID: number = 0;
    public teamLeaderID: number = 0;
    public faction: string = "";
    public color: number = 0;
    public hexColor: string = "";
    public handicap: number = 0;
    public startingPosition: { x: number, y: number, z: number } = { x: 0, y: 0, z: 0 };
    public startSpot: string | null = null;
    public startSpotLabel: string | null = null;

    public name: string = "";

    public static parse(elem: any): BarMatchTeam {
        const lut: number = Lut.lut(elem.color);

        let role: string | null = elem.startSpotLabel;
        if (role != null) {
            role = role.replaceAll("front", "Front").replaceAll("air", "Air").replaceAll("tech", "Tech").replaceAll("sea", "Sea");
        }

        return {
            ...elem,
            color: lut,
            hexColor: "#" + lut.toString(16).padStart(6, "0"),
            startSpotLabel: role,
        };
    }

}