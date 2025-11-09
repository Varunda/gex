
export class MapPlayCountEntry {
    public timestamp: Date | null = null;
    public gamemode: number = 0;
    public map: string = "";
    public count: number = 0;

    public static parse(elem: any): MapPlayCountEntry {
        return {
            ...elem,
            timestamp: elem.timestamp == null ? null : new Date(elem.timestamp)
        };
    }

}