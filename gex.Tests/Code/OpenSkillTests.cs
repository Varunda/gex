using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gex.Tests.Code {

    [TestClass]
    public class OpenSkillTests {

        [TestMethod]
        public void Test_Iter() {

            PlackettLuce model = new();
            /*
                Player  Team    Rating 	Uncertainty
                Crouxy 	  	1 	35.82 	2.66
                Deathkebab 		1 	35.71 	2.61
                ITz_HonestAbee 		1 	33.29 	2.65
                FrontLiner69er 	  	1 	30.84 	2.61
                Private_Gator 		1 	29.45 	2.63
                XxfuryxX 	  	1 	24.0 	3.18
                Concinus 		1 	23.77 	2.78
                alexandar 	  	1 	23.75 	2.65

                derkarl 		2 	40.02 	2.68
                Alphacure 		2 	35.46 	2.65
                NihilisticDuck73 	  	2 	35.41 	2.71
                [MADO]MyDude 	  	2 	31.88 	2.68
                AbyssWatcher777 	  	2 	27.07 	2.65
                Dudeney 	  	2 	24.74 	3.19
                Svinpals 	  	2 	19.69 	2.75
                Sujiro_Kimimame 	  	2 	18.43 	2.97 
            */

            ITeam team0 = new Team() {
                Players = [
                    model.Rating(mu: 35.82 + 2.66, sigma: 2.66),
                    model.Rating(mu: 35.71 + 2.61, sigma: 2.61),
                    model.Rating(mu: 33.29 + 2.65, sigma: 2.65),
                    model.Rating(mu: 30.84 + 2.61, sigma: 2.61),
                    model.Rating(mu: 29.45 + 2.63, sigma: 2.63),
                    model.Rating(mu: 24.00 + 3.18, sigma: 3.18),
                    model.Rating(mu: 23.77 + 2.78, sigma: 2.78),
                    model.Rating(mu: 23.75 + 2.65, sigma: 2.65),
                ]
            };

            ITeam team1 = new Team() {
                Players = [
                    model.Rating(mu: 40.02 + 2.68, sigma: 2.68),
                    model.Rating(mu: 35.46 + 2.65, sigma: 2.65),
                    model.Rating(mu: 35.41 + 2.71, sigma: 2.71),
                    model.Rating(mu: 31.88 + 2.68, sigma: 2.68),
                    model.Rating(mu: 27.07 + 2.65, sigma: 2.64),
                    model.Rating(mu: 24.74 + 3.19, sigma: 3.18),
                    model.Rating(mu: 19.69 + 2.75, sigma: 2.75),
                    model.Rating(mu: 18.43 + 2.97, sigma: 2.97),
                ]
            };

            List<double> winPreds = [.. model.PredictWin([team0, team1])];

            List<ITeam> afterTeams = [.. model.Rate([team0, team1])];
            for (int i = 0; i < afterTeams.Count; ++i) {
                Console.WriteLine($"Team {i}");
                for (int j = 0; j < afterTeams[i].Players.Count(); ++j) {
                    IRating p = afterTeams[i].Players.ElementAt(j);
                    Console.WriteLine($"\t{p.Mu} - {p.Sigma}: {p.Mu - p.Sigma}");
                }
            }
            List<double> winPreds2 = [.. model.PredictWin(afterTeams)];

            for (int i = 0; i < winPreds.Count; ++i) {
                Console.WriteLine($"Team {i}: {winPreds[i]} => {winPreds2[i]}");
            }
        }


    }
}
