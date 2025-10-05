
export class MatchPool {
    public id: number = 0;
    public name: string = "";
    public createdByID: number = 0;
    public timestamp: Date = new Date();

    public static parse(elem: any): MatchPool {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }

}