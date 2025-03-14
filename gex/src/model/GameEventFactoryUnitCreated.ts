
export class GameEventFactoryUnitCreated {
    public gameID: string = "";
    public frame: number = 0;
    public unitID: number = 0;
    public definitionID: number = 0;
    public teamID: number = 0;
    public factoryUnitID: number = 0;
    public factoryDefintion: number = 0;

    public static parse(elem: any): GameEventFactoryUnitCreated {
        return {
            ...elem
        };
    }
}
