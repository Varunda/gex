
export class GameEventUnitDamage {
    public gameID: string = "";
    public frame: number = 0;
    public unitID: number = 0;
    public definitionID: number = 0;
    public teamID: number = 0;
    public damageDealt: number = 0;
    public damageTaken: number = 0;

    public static parse(elem: any): GameEventUnitDamage {
        return {
            ...elem
        };
    }
}