
export class UnitIconAtlasJson {
    public timestamp: Date = new Date();
    public data: Map<string, number> = new Map();

    public static parse(elem: any): UnitIconAtlasJson {
        const map: Map<string, number> = new Map();
        for (const key of Object.keys(elem.data)) {
            map.set(key, elem.data[key]);
        }

        return {
            timestamp: new Date(elem.timestamp),
            data: map
        };
    }

}