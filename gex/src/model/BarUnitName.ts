
export class BarUnitName {

    public definitionName: string = "";
    public displayName: string = "";

    public static parse(elem: any): BarUnitName {
        return {
            definitionName: elem.definitionName,
            displayName: elem.displayName
        };
    }

}