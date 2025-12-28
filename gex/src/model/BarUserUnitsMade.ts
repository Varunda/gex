
export class BarUserUnitsMade {
    public userID: number = 0;
    public day: Date = new Date();
    public mapFilename: string = "";
    public definitionName: string = "";
    public unitName: string | null = null;
    public count: number = 0;
    public timestamp: Date = new Date();

    public static parse(elem: any): BarUserUnitsMade {
        return {
            userID: elem.userID,
            day: new Date(elem.day),
            mapFilename: elem.mapFilename,
            definitionName: elem.definitionName,
            unitName: elem.unitName,
            count: elem.count,
            timestamp: new Date(elem.timestamp)
        };
    }

}