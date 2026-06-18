
export class BarMapRotation {
    public name: string = "";
    public maps: string[] = [];

    public static parse(elem: any): BarMapRotation {
        return {
            ...elem
        };
    }

}