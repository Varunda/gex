import { ApmTournamentTeam } from "model/apm_tournament/ApmTournamentInfo";

export class TournamentMatchTeamNames {
    public tournamentID: string = "";
    public entries: TournamentMatchTeamNamesEntry[] = [];
}

export class TournamentMatchTeamNamesEntry {
    public gameID: string = "";
    public teams: ApmTournamentTeam[] = [];
}