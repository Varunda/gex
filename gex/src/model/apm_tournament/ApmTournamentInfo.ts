
export class ApmTournamentInfo {
    public id: string = "";
    public name: string = "";
    public slug: string = "";

    public teams: ApmTournamentTeam[] = [];
    public rounds: ApmTournamentRound[] = [];
    public matches: ApmTournamentMatch[] = [];

    public static parse(elem: any): ApmTournamentInfo {
        const info: ApmTournamentInfo = {
            id: elem.id,
            name: elem.name,
            slug: elem.slug,
            teams: elem.teams.map((iter: any) => ApmTournamentTeam.parse(iter)),
            rounds: elem.rounds.map((iter: any) => ApmTournamentRound.parse(iter)),
            matches: elem.matches.map((iter: any) => ApmTournamentMatch.parse(iter)),
        };

        info.teams.sort((a, b) => {
            return a.name.localeCompare(b.name);
        });

        return info;
    }

}

export class ApmTournamentRound {
    public id: string = "";
    public round: number = 0;
    public bestOf: number = 0;
    public note: string | null = null;

    public static parse(elem: any): ApmTournamentRound {
        return {
            ...elem
        }
    }
}

export class ApmTournamentMatch {
    public id: string = "";
    public roundID: string = "";
    public round: number = 0;
    public matchNumber: number = 0;
    public bestOf: number = 0;
    public winnerEntryID: string = "";

    public entries: ApmTournamentMatchEntry[] = [];
    public games: ApmTournamentMatchGame [] = [];

    public static parse(elem: any): ApmTournamentMatch {
        return {
            id: elem.id,
            roundID: elem.roundID,
            round: elem.round,
            matchNumber: elem.matchNumber,
            bestOf: elem.bestOf,
            winnerEntryID: elem.winnerEntryID,

            entries: elem.entries.map((iter: any) => ApmTournamentMatchEntry.parse(iter)),
            games: elem.games.map((iter: any) => ApmTournamentMatchGame.parse(iter)),
        }
    }
}

export class ApmTournamentMatchEntry {
    public entryID: string = "";
    public score: number = 0;

    public static parse(elem: any): ApmTournamentMatchEntry {
        return {
            ...elem
        };
    }
}

export class ApmTournamentMatchGame {
    public id: string = "";
    public gameNumber: number = 0;
    public startedAt: Date = new Date();
    public winnerEntryID: string = "";
    public map: string = "";

    public static parse(elem: any): ApmTournamentMatchGame {
        return {
            id: elem.id,
            gameNumber: elem.gameNumber,
            startedAt: new Date(elem.startedAt),
            winnerEntryID: elem.winnerEntryID,
            map: elem.map
        }
    }
}

export class ApmTournamentTeam {
    public id: string = "";
    public name: string = "";

    public members: ApmTournamentTeamMember[] = [];

    public static parse(elem: any): ApmTournamentTeam {
        return {
            id: elem.id,
            name: elem.name,
            members: elem.members.map((iter: any) => ApmTournamentTeamMember.parse(iter))
        }
    }
}

export class ApmTournamentTeamMember {
    public barUserID: number = 0;
    public id: string = "";
    public name: string = "";

    public static parse(elem: any): ApmTournamentTeamMember {
        return {
            ...elem
        }
    }
}