
export class GameEventUnitKilled {
    public gameID: string = "";
    public frame: number = 0;
    public unitID: number = 0;
    public definitionID: number = 0;
    public teamID: number = 0;
    public attackerID: number | null = null;
    public attackerDefinitionID: number | null = null;
    public attackerTeam: number | null = null;
    public weaponDefinitionID: number = 0;
    public killedX: number = 0;
    public killedY: number = 0;
    public killedZ: number = 0;
    public attackerX: number | null = null;
    public attackerY: number | null = null;
    public attackerZ: number | null = null;

    public static parse(elem: any): GameEventUnitKilled {
        return {
            ...elem
        };
    }

}