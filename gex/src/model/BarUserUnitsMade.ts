
export class BarUserUnitsMade {
    public userID: number = 0;
    public definitionName: string = "";
    public unitName: string = "";
    public count: number = 0;

    public static parse(elem: any): BarUserUnitsMade {
        return {
            userID: elem.userID,
            definitionName: elem.definitionName,
            unitName: elem.unitName,
            count: elem.count
        };
    }

}