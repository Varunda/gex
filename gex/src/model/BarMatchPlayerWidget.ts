
export class BarMatchPlayerWidget {
    public gameID: string = "";
    public userID: number = 0;
    public hash: string = "";
    public state: number = 0;
    public name: string = "";
    public author: string = "";
    public description: string = "";

    public static parse(elem: any): BarMatchPlayerWidget {
        return {
            ...elem
        };
    }

}