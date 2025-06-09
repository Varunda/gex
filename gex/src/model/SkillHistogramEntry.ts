
export class SkillHistogramEntry {
    public skillLowerBound: number = 0;
    public playerCount: number = 0;

    public static parse(elem: any): SkillHistogramEntry {
        return {
            ...elem
        };
    }
}