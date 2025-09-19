
export class BarUserUnitsMade {
    public userID: number = 0;
    public timestamp: Date = new Date();
    public definitionName: string = "";
    public unitName: string = "";
    public count: number = 0;

    public static parse(elem: any): BarUserUnitsMade {
        return {
            userID: elem.userID,
            timestamp: new Date(elem.timestamp),
            definitionName: elem.definitionName,
            unitName: elem.unitName,
            count: elem.count
        };
    }

}