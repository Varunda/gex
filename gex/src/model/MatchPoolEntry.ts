
export class MatchPoolEntry {
    public poolID: number = 0;
    public matchID: string = "";
    public addedByID: number = 0;
    public timestamp: Date = new Date();

    public static parse(elem: any): MatchPoolEntry {
        return {
            poolID: elem.poolID,
            matchID: elem.matchID,
            addedByID: elem.addedByID,
            timestamp: new Date(elem.timestamp)
        };
    }

}