using gex.Common.Code.Constants;
using gex.Common.Models.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models.Match {

    public class BarMatchEntity {

        public string Name { get; set; } = "";

        public string HexColor { get; set; } = "";

        public int SortOrder { get; set; } = 0;

        public HashSet<int> TeamIDs { get; set; } = [];

        public static List<BarMatchEntity> GetEntities(BarMatch match) {
            List<BarMatchEntity> ents = [];

            foreach (BarMatchTeam team in match.Teams) {
                IEnumerable<BarMatchPlayer> players = match.Players.Where(iter => iter.TeamID == team.TeamID);
                IEnumerable<BarMatchAiPlayer> ais = match.AiPlayers.Where(iter => iter.TeamID == team.TeamID);

                List<string> names = players.Select(iter => iter.Name).ToList();
                names.AddRange(ais.Select(iter => iter.Name));

                ents.Add(new BarMatchEntity() {
                    Name = string.Join(" & ", names),
                    TeamIDs = [ team.TeamID ],
                    HexColor = $"#{TeamColorLut.Lut.GetValueOrDefault(team.Color, team.Color).ToString("X2").PadLeft(6, '0')}",
                    SortOrder = team.TeamID,
                });
            }

            if (match.Gamemode == BarGamemode.SMALL_TEAM || match.Gamemode == BarGamemode.LARGE_TEAM
                || match.Gamemode == BarGamemode.TEAM_FFA || match.Gamemode == BarGamemode.DEFAULT) {

                foreach (BarMatchAllyTeam allyTeam in match.AllyTeams) {
                    BarMatchTeam? team = match.Teams.Where(iter => iter.AllyTeamID == allyTeam.AllyTeamID).OrderBy(iter => iter.TeamID).FirstOrDefault();

                    ents.Add(new BarMatchEntity() {
                        Name = $"Team {allyTeam.AllyTeamID + 1}",
                        TeamIDs = new HashSet<int>(
                            match.Teams.Where(iter => iter.AllyTeamID == allyTeam.AllyTeamID).Select(iter => iter.TeamID)
                        ),
                        HexColor = $"#{TeamColorLut.Lut.GetValueOrDefault(team?.Color ?? 0, team?.Color ?? 0).ToString("X2").PadLeft(6, '0')}",
                        SortOrder = allyTeam.AllyTeamID,
                    });
                }
            }

            ents.Sort((BarMatchEntity aEnt, BarMatchEntity bEnt) => {
                // a=ally team, b=ally team => smaller ally team
                // a=ally team, b=team      => if b is in team a, then a>b, else b>a
                // a=team,      b=ally team => if a is in team b, then b>a, else a>b
                // a=team,      b=team      => if a and b are on the same team, sort by label, else smaller team

                int res = 0;

                int aId = aEnt.SortOrder;
                int bId = bEnt.SortOrder;

                bool aIsAt = aEnt.TeamIDs.Count > 1;
                bool bIsAt = bEnt.TeamIDs.Count > 1;

                if (aIsAt == true && bIsAt == true) {
                    res = aId - bId;
                } else if (aIsAt == true && bIsAt == false) {
                    int bTeam = match.Teams.FirstOrDefault(iter => iter.TeamID == bId)?.AllyTeamID ?? -1;

                    if (bTeam == aId) {
                        res = -1; // A is an ally team, and B is part of this team, so A is smaller (higher in list)
                    } else {
                        // A is an ally team, but B is not part of this team, so smaller team wins
                        // if A is ally team 1, and B is on ally team 2, B goes after A (1)
                        res = aId - bTeam;
                    }
                } else if (aIsAt == false && bIsAt == true) {
                    int aTeam = match.Teams.FirstOrDefault(iter => iter.TeamID == aId)?.AllyTeamID ?? -1;

                    if (aTeam == bId) {
                        res = 1; // B is an ally team, and A is part of this team, so B is smaller (higher in list)
                    } else {
                        // B is an ally team, but A is not part of this team, so smaller team wins
                        // if B is ally team 1, and A is on ally team 2, then B goes after A (1)
                        res = aTeam - bId;
                    }
                } else if (aIsAt == false && bIsAt == false) {
                    int aTeam = match.Teams.FirstOrDefault(iter => iter.TeamID == aId)?.AllyTeamID ?? -1;
                    int bTeam = match.Teams.FirstOrDefault(iter => iter.TeamID == bId)?.AllyTeamID ?? -1;

                    if (aTeam == bTeam) {
                        res = aEnt.Name.CompareTo(bEnt.Name);
                    } else {
                        res = aTeam - bTeam;
                    }
                } else {
                    throw new InvalidOperationException($"unchecked logic state");
                }

                return res;
            });

            return ents;
        }

        public override bool Equals(object? obj) {
            return obj is BarMatchEntity entity
                   && Name == entity.Name
                   && HexColor == entity.HexColor
                   && EqualityComparer<HashSet<int>>.Default.Equals(TeamIDs, entity.TeamIDs);
        }

        public static bool operator == (BarMatchEntity? left, BarMatchEntity? right) {
            if (left is null) {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator != (BarMatchEntity? left, BarMatchEntity? right) {
            return !(left == right);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Name, HexColor, TeamIDs);
        }

    }
}
