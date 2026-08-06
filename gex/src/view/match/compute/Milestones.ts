import { BarMatch } from "model/BarMatch";
import { GameEventUnitDef } from "model/GameEventUnitDef";
import { GameOutput } from "model/GameOutput";

export class Milestone {
    public frame: number = 0;
    public entity: string = "";
    public interest: number = 0;
    public action: string = "";

    public static compute(match: BarMatch, output: GameOutput, entity: string): Milestone[] {
        const allyTeamMapping: Map<number, number> = new Map();
        for (const player of match.players) {
            allyTeamMapping.set(player.teamID, player.allyTeamID);
        }

        const interest: Milestone[] = [];

        let t1made: boolean = false;
        let t2made: boolean = false;
        let t3made: boolean = false;
        let firstAfus: boolean = false;
        let vehicleSwap: boolean = !(match.players.length == 2 && match.allyTeams.length == 2); // only interesting for duels
        let geoMade: boolean = false;
        let ageoMade: boolean = false;

        let botStart: boolean = false;

        for (const ev of output.unitsCreated) {

            if (output.hasUnitCompleted == true && ev.completed == 0) {
                continue;
            }

            if (entity.startsWith("team-")) {
                const teamID: number = Number.parseInt(entity.split("-")[1]);
                if (ev.teamID != teamID) {
                    continue;
                }
            } else if (entity.startsWith("ally-team-")) {
                const allyTeamID: number = Number.parseInt(entity.split("-")[2]);
                if (allyTeamMapping.get(ev.teamID) != allyTeamID) {
                    continue;
                }
            } else {
                throw `unchecked SelectedEntity: ${entity}`;
            }

            const def: GameEventUnitDef | undefined = output.unitDefinitions.get(ev.definitionID);
            if (def == undefined) {
                continue;
            }

            if (vehicleSwap == false && botStart == false) {
                if (def.isFactory == true && def.name == "Bot Lab" && def.unitGroup == "builder") {
                    botStart = true;
                }
            } else if (vehicleSwap == false && botStart == true) {
                if (def.isFactory == true && def.name == "Vehicle Plant" && def.unitGroup == "builder") {
                    interest.push({
                        entity: entity,
                        frame: ev.completed || ev.frame,
                        action: "Bot -> Vehicle swap",
                        interest: 8
                    });
                    vehicleSwap = true;
                }
            }

            if (t1made == false) {
                if (def.isFactory == true && def.isFactory == true && def.unitGroup == "builder") {
                    interest.push({
                        entity: entity,
                        frame: ev.completed || ev.frame,
                        action: "T1 made",
                        interest: 1
                    });
                    t1made = true;
                }
            }

            if (t2made == false) {
                if (def.isFactory == true && def.isFactory == true && def.unitGroup == "buildert2" && def.speed == 0) {
                    interest.push({
                        entity: entity,
                        frame: ev.completed || ev.frame,
                        action: "T2 made",
                        interest: 10
                    });
                    t2made = true;
                }
            }

            if (t3made == false) {
                if (def.isFactory == true && def.isFactory == true && def.unitGroup == "buildert3" && def.speed ==  0) {
                    interest.push({
                        entity: entity,
                        frame: ev.completed || ev.frame,
                        action: "Gantry made",
                        interest: 5
                    });
                    t3made = true;
                }
            }

            if (firstAfus == false) {
                if (def.energyProduction > 2000 && def.buildTime > 100000) {
                    interest.push({
                        entity: entity,
                        frame: ev.completed || ev.frame,
                        action: "First AFUS",
                        interest: 5
                    });
                    firstAfus = true;
                }
            }

            if (geoMade == false) {
                if (def.name.indexOf("Geothermal") > -1 && def.unitGroup == "energy" && def.energyProduction < 800) {
                    interest.push({
                        entity: entity,
                        frame: ev.completed || ev.frame,
                        action: "Geo built",
                        interest: 3
                    });
                    geoMade = true;
                }
            }

            if (ageoMade == false) {
                if (def.name.indexOf("Geothermal") > -1 && def.unitGroup == "energy" && def.energyProduction > 800) {
                    interest.push({
                        entity: entity,
                        frame: ev.completed || ev.frame,
                        action: "Adv. Geo built",
                        interest: 4
                    });
                    ageoMade = true;
                }
            }
        }

        return interest.sort((a, b) => b.interest - a.interest);
    }

}

