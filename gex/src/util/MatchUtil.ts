import { BarMatch } from "model/BarMatch";
import { BarMatchPlayer } from "model/BarMatchPlayer";
import { BarMatchTeam } from "model/BarMatchTeam";

export class MatchUtil {

    public static isQuantumMode(match: BarMatch): boolean {
        return match.teams.filter(iter => match.players.filter(i2 => i2.teamID == iter.teamID).length > 1).length > 1;
    }


    public static getPlayersOfTeam(match: BarMatch, teamID: number): BarMatchPlayer[] {
        return match.players.filter(iter => iter.teamID == teamID);
    }

    public static getTeamOfPlayer(match: BarMatch, playerID: number): BarMatchTeam | null {
        const player: BarMatchPlayer | undefined = match.players.find(iter => iter.playerID == playerID);
        if (player == undefined) {
            return null;
        }

        const team: BarMatchTeam | undefined = match.teams.find(iter => iter.teamID == player.teamID);
        if (team == undefined) {
            return null;
        }

        // check that this is a team with only a single player (not quantum mode)
        const teamPlayers: BarMatchPlayer[] = match.players.filter(iter => iter.teamID == team.teamID);
        if (teamPlayers.length == 1) {
            return team;
        }

        return null;
    }
    
}