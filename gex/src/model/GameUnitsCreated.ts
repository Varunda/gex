
export class GameUnitsCreated {

    public gameID: string = "";
    public teamID: number = 0;
    public userID: number = 0;
    public definitionName: string = "";
    public unitName: string = "";
    public count: number = 0;
    public timestamp: Date = new Date();

    public static parse(elem: any): GameUnitsCreated {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }

}