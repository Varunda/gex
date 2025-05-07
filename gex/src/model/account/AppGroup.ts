
export class AppGroup {
    public id: number = 0;
    public name: string = "";
    public hexColor: string = "";
    public implies: number[] = [];

    public static parse(elem: any): AppGroup {
        return {
            ...elem
        }
    }

}
