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
