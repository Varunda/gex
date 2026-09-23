
export class ArmyValuePosition {
    public frame: number = 0;
    public entityID: string = "";
    public entries: ArmyValuePositionEntry[] = [];
}

export class ArmyValuePositionEntry {
    public unitID: number = 0;
    public metalValue: number = 0;
    public x: number = 0;
    public z: number = 0;
}