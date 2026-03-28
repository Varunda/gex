
export class GameEventUnitDamage {
    public gameID: string = "";
    public frame: number = 0;
    public unitID: number = 0;
    public definitionID: number = 0;
    public teamID: number = 0;
    public damageDealt: number = 0;
    public damageTaken: number = 0;
    public experience: number = 0;
    public rank: number = 0;

    public static parse(elem: any): GameEventUnitDamage {
        return {
            ...elem,
            rank: Math.min(16, Math.floor(elem.experience / 0.05))
        };
    }
}