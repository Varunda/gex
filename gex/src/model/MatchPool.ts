import { ApmTournamentInfo } from "./apm_tournament/ApmTournamentInfo";

export class MatchPool {
    public id: number = 0;
    public name: string = "";
    public createdByID: number = 0;
    public timestamp: Date = new Date();
    public unlisted: boolean = false;
    public hideUntil: Date | null = null;
    public tournamentInfos: ApmTournamentInfo[] = [];

    public static parse(elem: any): MatchPool {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp),
            hideUntil: elem.hideUntil == null ? null : new Date(elem.hideUntil),
            tournamentInfos: elem.tournamentInfos.map((iter: any) => ApmTournamentInfo.parse(iter)),
        };
    }

}