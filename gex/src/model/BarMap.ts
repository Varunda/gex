
export class BarMap {
    public id: number = 0;
    public name: string = "";
    public fileName: string = "";
    public description: string = "";
    public tidalStrength: number = 0;
    public maxMetal: number = 0;
    public extractorRadius: number = 0;
    public minimumWind: number = 0;
    public maximumWind: number = 0;
    public width: number = 0;
    public height: number = 0;
    public author: string = "";

    public static parse(elem: any): BarMap {
        return {
            ...elem
        };
    }

}