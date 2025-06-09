
export class MapPlayCountEntry {
    public gamemode: number = 0;
    public map: string = "";
    public count: number = 0;

    public static parse(elem: any): MapPlayCountEntry {
        return {
            ...elem
        };
    }

}