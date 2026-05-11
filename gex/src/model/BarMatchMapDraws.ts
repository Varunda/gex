
export type BarMatchMapDraw = BarMatchMapDrawPoint | BarMatchMapDrawLine | BarMatchMapDrawErase;

export class BarMatchMapDrawBase {
    public playerID: number = 0;
    public gameTime: number = 0;
    //public action: string = "";
    public x: number = 0;
    public z: number = 0;

    public static parse(elem: any): BarMatchMapDraw {

        if (elem.action == "point") {
            return BarMatchMapDrawPoint.parse(elem);
        } else if (elem.action == "line") {
            return BarMatchMapDrawLine.parse(elem);
        } else if (elem.action == "erase") {
            return BarMatchMapDrawErase.parse(elem);
        } else {
            throw `unchecked BarMatchMapDraw action '${elem.action}'`;
        }
    }
}

export class BarMatchMapDrawPoint extends BarMatchMapDrawBase {
    public action = "point";
    public label: string = "";
    public fromLua: number = 0;

    public static parse(elem: any): BarMatchMapDrawPoint {
        return {
            ...elem
        };
    }
}

export class BarMatchMapDrawLine extends BarMatchMapDrawBase {
    public action = "line";
    public endX: number = 0;
    public endZ: number = 0;
    public fromLua: number = 0;

    public static parse(elem: any): BarMatchMapDrawLine {
        return {
            ...elem
        };
    }
}

export class BarMatchMapDrawErase extends BarMatchMapDrawBase {
    public action = "erase";
    // same parameters as base class

    public static parse(elem: any): BarMatchMapDrawErase {
        return {
            ...elem
        };
    }
}