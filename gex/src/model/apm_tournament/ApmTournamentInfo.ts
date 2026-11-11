
export class ApmTournamentInfo {
    public id: string = "";
    public name: string = "";
    public slug: string = "";

    public static parse(elem: any): ApmTournamentInfo {
        return {
            ...elem
        };
    }

}
