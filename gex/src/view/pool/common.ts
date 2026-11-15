import { ApmTournamentInfo, ApmTournamentMatch, ApmTournamentMatchEntry, ApmTournamentMatchGame, ApmTournamentTeam } from "model/apm_tournament/ApmTournamentInfo";

export class RoundMatches {
    public roundID: string = "";
    public round: number = 0;

    public matches: RoundMatchGame[] = [];
}

export class RoundMatchGame {
    public id: string = "";
    public matchNumber: number = 0;
    public winnerID: string = "";
    public teams: ApmTournamentTeam[] = [];
    public winner: ApmTournamentTeam | null = null;
    public games: ApmTournamentMatchGame[] = [];

    public matchInfo: ApmTournamentMatch = new ApmTournamentMatch();
}
