

export class BarPlayerCommand {

    public id: number = 0;
    public fullGameTime: number = 0;
    public playerID: number = 0;
    public unitIDs: number[] = [];

    public static parse(elem: any): BarPlayerCommand {
        return {
            ...elem
        };
    }

}