
export class BarMapData {
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

    public header: BarMapFileHeader = new BarMapFileHeader();

    public static parse(elem: any): BarMapData {
        return {
            ...elem,
            header: BarMapFileHeader.parse(elem.header)
        };
    }

}

export class BarMapFileHeader {

    public id: number = 0;
    public width: number = 0;
    public height: number = 0;
    public squareSize: number = 0;
    public texelsPerSquare: number = 0;
    public tileSize: number = 0;
    public minHeight: number = 0;
    public maxHeight: number = 0;
    public heightMap: number[] = [];

    public static parse(elem: any): BarMapFileHeader {
        return {
            ...elem
        }
    }

}