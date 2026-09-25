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
                    HexColor = $"#{TeamColorLut.Lut.GetValueOrDefault(team.Color, team.Color).ToString("X2").PadLeft(6, '0')}"
                });
            }

            foreach (BarMatchAllyTeam allyTeam in match.AllyTeams) {
                BarMatchTeam? team = match.Teams.Where(iter => iter.AllyTeamID == allyTeam.AllyTeamID).OrderBy(iter => iter.TeamID).FirstOrDefault();

                ents.Add(new BarMatchEntity() {
                    Name = $"Team {allyTeam.AllyTeamID + 1}",
                    TeamIDs = new HashSet<int>(
                        match.Teams.Where(iter => iter.AllyTeamID == allyTeam.AllyTeamID).Select(iter => iter.TeamID)
                    ),
                    HexColor = $"#{TeamColorLut.Lut.GetValueOrDefault(team?.Color ?? 0, team?.Color ?? 0).ToString("X2").PadLeft(6, '0')}"
                });
            }

            return ents;
        }

    }
}
