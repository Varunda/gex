

export class BarUserSkill {
    public userID: number = 0;
    public gamemode: number = 0;
    public skill: number = 0;
    public skillUncertainty: number = 0;
    public lastUpdated: Date = new Date();

    public static parse(elem: any): BarUserSkill {
        return {
            ...elem,
            lastUpdated: new Date(elem.lastUpdated)
        };
    }

}