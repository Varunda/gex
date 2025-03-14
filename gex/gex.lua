local headless = (Spring.GetConfigInt("Headless", 0) ~= 0)
Spring.Echo("[Gex] started gex!", headless)

local UNIT_DEF_NAMES = {}
local UNIT_DEF_METAL = {}
local UNIT_DEF_IS_COMMANDER = {}
local frame = 0
local timer
local commanders = {}

function writeJson(action, data, includeFrame)
    file = io.open("actions.json", "a")
    io.output(file)

    writeJsonRaw(action, data, includeFrame)

    io.flush()
    io.close()
end

function writeJsonRaw(action, data, includeFrame)
    io.write("{\"action\":\"")
    io.write(action)
    io.write("\"")

    if (includeFrame ~= false) then
		io.write(",\"frame\":")
		io.write(frame)
    end

    if (#data > 0) then
        io.write(",")
    end

    for i=1,#data do
        local iter = data[i]
        io.write("\"")
        io.write(iter[1])
        io.write("\":")
        if (type(iter[2]) == "string") then
            io.write("\"")
            local v = (iter[2] or "null"):gsub("\"", "\\\"")
            io.write(v)
            io.write("\"")
        elseif (type(iter[2]) == "boolean") then
            if (iter[2] == true) then
                io.write("true")
            else
                io.write("false")
            end
        else
            io.write(iter[2] or "null")
        end

        if (i < #data) then
            io.write(",")
        end
    end
    io.write("}\n")
end

function widget:GetInfo()
    return {
        name    = "game_event_extractor",
        desc    = "extracts game events to a file for further parsing",
        author  = "varunda",
        date    = "2025",
        license = "MIT",
        layer   = 0,
        enabled = true
    }
end

function widget:Initialize()
    Spring.Echo("[Gex] starting game event extractor (gex)")

    local headless = (Spring.GetConfigInt("Headless", 0) ~= 0)
    if (not headless) then
        Spring.Echo("[Gex] not running gex, Headless is", headless)
        return
    end

    file = io.open("actions.json", "w")
    io.output(file)
    io.write("{\"action\":\"init\",\"frame\":0}\n")

	for k,v in pairs(UnitDefs) do
        -- all the number and string props (not tables or bools i guess?)
        -- {
        -- "action":"unit_def","frame":0,"defID":1,"defName":"armaak","tooltip":"Advanced Amphibious Anti-Air Bot","name":"Archangel","transportCapacity":0,
        -- "slideTolerance":0,"scriptPath":"scripts/Units/ARMAAK.cob","nanoColorG":0.69999998807907,"flareEfficiency":0.5,"armoredMultiple":1,"seismicSignature":0,"radarDistance":0,
        -- "scriptName":"scripts/Units/ARMAAK.cob","decloakDistance":0,"selfDExplosion":"mediumexplosiongenericselfd-phib","flareSalvoDelay":0,"trackWidth":32,
        -- "windGenerator":0,"maxBank":0.80000001192093,"radarRadius":0,"transportUnloadMethod":0,"maxAileron":0.014999999664724,"airSightDistance":925,"maxWeaponRange":875,
        -- "flareSalvoSize":4,"maxRudder":0.0040000001899898,"metalCost":520,"speedToFront":0.070000000298023,"radarDistanceJam":0,"harvestEnergyStorage":0,"makesMetal":0,
        -- "unitFallSpeed":0,"cruiseAltitude":0,"transportMass":100000,"sonarDistance":0,"fallSpeed":0.20000000298023,"highTrajectoryType":0,"buildSpeed":0,"radius":18,
        -- "nanoColorB":0.20000000298023,"resurrectSpeed":0,"modeltype":"s3o","turnRate":1174.1500244141,"id":1,"humanName":"","energyUpkeep":0,"frontToSpeed":0.10000000149012,
        -- "cloakCostMoving":0,"flareDropVectorX":0,"cloakCost":0,"energyCost":5600,"buildpicname":"ARMAAK.DDS","flareDropVectorZ":0,"buildingDecalSizeY":4,"flareReloadTime":5,
        -- "terraformSpeed":0,"corpse":"armaak_dead","buildeeBuildRadius":-1,"reclaimSpeed":0,"flankingBonusDirX":0,"repairSpeed":0,"totalEnergyOut":0,"seismicDistance":0,
        -- "sonarJamRadius":0,"flankingBonusMobilityAdd":0.0099999997764826,"tooltip":"armaak","waterline":0,"kamikazeDist":0,"dlHoverFactor":-1,"buildPic":"ARMAAK.DDS",
        -- "flareDropVectorY":0,"energyStorage":0,"energyMake":0,"cobID":-1,"verticalSpeed":3,"buildTime":7000,"flankingBonusMode":1,"fireState":-1,"autoHeal":0,
        -- "groundFrictionCoefficient":0.0099999997764826,"trackStrength":0,"description":"armaak","idleTime":1800,"kamikazeDistance":0,"flareTime":90,"rollingResistanceCoefficient":0.050000000745058,
        -- "metalUpkeep":0,"maxRepairSpeed":0,"deathExplosion":"mediumexplosiongeneric-phib","flankingBonusDirY":0,"selfDestructCountdown":5,"radarEmitHeight":40,
        -- "trackOffset":0,"sightDistance":400,"health":1130,"modelname":"Units/ARMAAK.s3o","rSpeed":0,"mass":520,"speed":47.400001525879,"extractsMetal":0,
        -- "flankingBonusMax":2,"seismicRadius":0,"buildingDecalSizeX":4,"iconType":"armaak","maxElevator":0.0099999997764826,"sightEmitHeight":40,"myGravity":0.40000000596046,
        -- "moveState":0,"transportSize":0,"harvestMetalStorage":0,"sonarRadius":0,"metalStorage":0,"minCollisionSpeed":2.5,"wantedHeight":0,"jammerRadius":0,
        -- "maxThisUnit":32000,"maxDec":0.64859998226166,"selfDCountdown":5,"flankingBonusMin":1,"flareDelay":0.30000001192093,"flankingBonusDirZ":1,"zsize":4,"loadingRadius":220,
        -- "maxPitch":0.44999998807907,"metalMake":0,"airLosRadius":925,"maxCoverage":0,"captureSpeed":0,"buildDistance":128,"sonarDistanceJam":0,
        -- "crashDrag":0.0049999998882413,"turnInPlaceSpeedLimit":1.042799949646,"modelpath":"objects3d/Units/ARMAAK.s3o","maxAcc":0.13799999654293,"nanoColorR":0.20000000298023,
        -- "upDirSmoothing":0,"maxWaterDepth":10000000,"buildingDecalDecaySpeed":0.10000000149012,"tidalGenerator":0,"buildingDecalType":-1,"name":"armaak",
        -- "wingDrag":0.070000000298023,"extractRange":0,"wingAngle":0.079999998211861,"xsize":4,"turnRadius":500,"minWaterDepth":-10000000,
        -- "losHeight":40,"trackStretch":1,"maxHeightDif":15.354561805725,"wreckName":"armaak_dead","height":30,"idleAutoHeal":2.5,"power":613.33331298828,
        -- "armorType":12,"losRadius":400,"cost":613.33331298828,"translatedHumanName":"Archangel","translatedTooltip":"Advanced Amphibious Anti-Air Bot"
        -- }
        writeJsonRaw("unit_def", {
            -- basic stuff
            { "defID", k },
            { "defName", v.name },
            { "tooltip", v.translatedTooltip },
            { "name", v.translatedHumanName },
            { "metalCost", v.metalCost },
            { "energyCost", v.energyCost },
            { "health", v.health },
            { "speed", v.speed },
            { "buildTime", v.buildTime },
            { "unitGroup", v.customParams.unitgroup or "" },
            { "buildPower", v.buildSpeed },

            -- eco info
            { "metalMake", v.metalMake },
            { "isMetalExtractor", v.customParams.metal_extractor ~= nil },
            { "extractsMetal", v.extractsMetal },
            { "metalStorage", v.metalStorage },
            { "windGenerator", v.windGenerator },
            { "tidalGenerator", v.tidalGenerator },
            { "energyProduction", v.totalEnergyOut },
            { "energyUpkeep", v.energyUpkeep },
            { "energyStorage", v.energyStorage },

            -- why are these 2 numbers? can customParams only be string?
            { "energyConversionCapacity", tonumber(v.customParams.energyconv_capacity or "0") },
            { "energyConversionEfficiency", tonumber(v.customParams.energyconv_efficiency or "0") },

            -- combat stuff
            { "sightDistance", v.sightDistance },
            { "airSightDistance", v.airSightDistance },
            { "attackRange", v.maxWeaponRange },
            { "isCommander", v.customParams.iscommander ~= nil },
            { "isReclaimer", v.isBuilder and not v.isFactory },
            { "isFactory", v.isFactory },
            { "weaponCount", #v.weapons }
        })

        UNIT_DEF_NAMES[k] = v["name"]
        UNIT_DEF_METAL[k] = v["metalCost"]
        UNIT_DEF_IS_COMMANDER[k] = v.customParams.iscommander ~= nil
	end
    io.flush()
    io.close(file)

    Spring.SendCommands(
        "setmaxspeed 9999", "setminspeed 9999", "hideinterface"
    )

    Spring.SendCommands("skip 1")
end

function widget:GameStart()
    timer = Spring.GetTimer()
    writeJson("start", { })
end

function widget:GameID(gameID)
    writeJson("game_id", {
        { "gameID", gameID }
    })
end

local STAT_DELTA = 0

function widget:GameFrame(n)
    frame = n

    if (frame % 150 == 0) then
        local _, _, _, actual_thing_i_care_about, _, _, _ = Spring.GetWind()

        writeJson("wind_update", {
            { "value", actual_thing_i_care_about }
        })

        for _,unitID in pairs(commanders) do
            local posx, posy, posz = Spring.GetUnitPosition(unitID)
            writeJson("commander_position_update", {
                { "unitID", unitID },
                { "posX", posx, },
                { "posY", posy, },
                { "posZ", posz, }
            })
        end
    end

    if (frame % 300 == 0) then
        local teamList = Spring.GetTeamList()
        for _,teamID in ipairs(teamList) do
            if (teamID ~= Spring.GetGaiaTeamID()) then
                local units = Spring.GetTeamUnits(teamID)
                local total_metal_cost = 0
                for k,v in pairs(units) do
                    local uid = Spring.GetUnitDefID(v)
                    total_metal_cost = total_metal_cost + (UNIT_DEF_METAL[uid] or 0)
                end

                writeJson("army_value_update", {
                    { "teamID", teamID },
                    { "value", total_metal_cost }
                })
            end
        end
    end
end

function widget:GotChatMsg(msg, playerID)
    writeJson("chat", {
        { "playerID", playerID },
        { "msg", msg }
    })
end

function widget:TeamDied(teamID)
    writeJson("team_died", {
        { "teamID", teamID }
    })
end

function widget:GameOver(winningAllyTeams)
    local time = Spring.DiffTimers(Spring.GetTimer(), timer)

    local data = {
        { "realtime", time },
        { "ingame", Spring.GetGameSeconds() },
        { "winners", winningAllyTeams }
    }

    writeJson("end", data)

    local teamList = Spring.GetTeamList()
    for _,teamID in ipairs(teamList) do
        if (teamID ~= Spring.GetGaiaTeamID()) then
            local range = Spring.GetTeamStatsHistory(teamID)
            local history = Spring.GetTeamStatsHistory(teamID, 0, range)

            if (history) then

                for i = 1,range do
                    data = {
                        { "teamID", teamID }
                    }

                    for k,v in pairs(history[i]) do
                        table.insert(data, { k, v })
                    end

                    writeJson("team_stats", data, false)
                end
            end
        end
    end

    Spring.Echo("[Gex] game over, force quitting")
    Spring.SendCommands("quitforce")

end

function widget:UnitCreated(unitID, unitDefID, teamID)

    if (UNIT_DEF_IS_COMMANDER[unitDefID] == true) then
        Spring.Echo("commander unit created", unitID, "defID", unitDefID)
		commanders[unitID] = unitID
    end

    local x, y, z = Spring.GetUnitPosition(unitID)

    writeJson("unit_created", {
        { "unitID", unitID },
        { "teamID", teamID },
        { "defID", unitDefID },
        { "defName", UNIT_DEF_NAMES[unitDefID] },
        { "unit_x", x },
        { "unit_y", y },
        { "unit_z", z },
    })
end

function widget:UnitDestroyed(unitID, unitDefID, teamID, attackerID, attackerDefID, attackerTeam, weaponDefID)

    if (commanders[unitID] ~= nil) then
        commanders[unitID] = nil
    end

    local kx, ky, kz = Spring.GetUnitPosition(unitID)
    local ax, ay, az = nil, nil, nil
    if (attackerID ~= nil) then
        ax, ay, az = Spring.GetUnitPosition(attackerID)
    end

    writeJson("unit_killed", {
        { "unitID", unitID },
        { "teamID", teamID },
        { "defID", unitDefID },
        { "defName", UNIT_DEF_NAMES[unitDefID] },
        { "attackerID", attackerID },
        { "attackerDefID", attackerDefID },
        { "attackerTeam", attackerTeam },
        { "weaponDefID", weaponDefID },
        { "killed_x", kx },
        { "killed_y", ky },
        { "killed_z", kz },
        { "attacker_x", ax },
        { "attacker_y", ay },
        { "attacker_z", az }
    })
end

function widget:UnitLoaded(unitID, unitDefID, unitTeam, transportID, transportTeam)
    local x, y, z = Spring.GetUnitPosition(unitID)

    writeJson("transport_loaded", {
        { "unitID", unitID },
        { "defID", unitDefID },
        { "teamID", unitTeam },
        { "transportUnitID", transportID },
        { "transportTeamID", transportTeam },
        { "unitX", x },
        { "unitY", y },
        { "unitZ", z }
    })
end

function widget:UnitUnloaded(unitID, unitDefID, unitTeam, transportID, transportTeam)
    local x, y, z = Spring.GetUnitPosition(unitID)

    writeJson("transport_unloaded", {
        { "unitID", unitID },
        { "defID", unitDefID },
        { "teamID", unitTeam },
        { "transportUnitID", transportID },
        { "transportTeamID", transportTeam },
        { "unitX", x },
        { "unitY", y },
        { "unitZ", z }
    })
end

function widget:UnitFromFactory(unitID, unitDefID, unitTeam, factID, factDefID, userOrders)
    writeJson("factory_unit_created", {
        { "unitID", unitID },
        { "defID", unitDefID },
        { "teamID", unitTeam },
        { "factoryID", factID },
        { "factoryDefID", factDefID }
    })
end

function widget:ProjectileCreated(projID, projOwnerID, weaponDefID)
    local x, y, z = Spring.GetUnitPosition(projOwnerID)

    writeJson("projectile_created", {
        { "projectileID", projID },
        { "projectileOwnerID", projOwnerID },
        { "weaponDefID", weaponDefID },
        { "owner_x", x },
        { "owner_y", y },
        { "owner_z", z },
    })
end

function widget:ProjectileDestroyed(projID, projOwnerID, weaponDefID)
    local x, y, z = Spring.GetUnitPosition(projOwnerID)

    writeJson("projectile_destroyed", {
        { "projectileID", projID },
        { "projectileOwnerID", projOwnerID },
        { "weaponDefID", weaponDefID },
        { "owner_x", x },
        { "owner_y", y },
        { "owner_z", z },
    })
end

function widget:UnitDamaged(unitID, unitDefID, teamID, damage, paralyzer, weaponDefID, projectileID, attackerID, attackerDefID, attackerTeam)
    if (attackerID == nil) then
        return
    end

    local kx, ky, kz = Spring.GetUnitPosition(unitID)
    local ax, ay, az = nil, nil, nil
    if (attackerID ~= nil) then
        ax, ay, az = Spring.GetUnitPosition(attackerID)
    end

    writeJson("unit_damaged", {
        { "unitID", unitID },
        { "teamID", teamID },
        { "damage", damage },
        { "defID", unitDefID },
        { "defName", UNIT_DEF_NAMES[unitDefID] },
        { "paralyzer", paralyzer },
        { "projectileID", projectileID },
        { "attackerID", attackerID },
        { "attackerDefID", attackerDefID },
        { "attackerTeam", attackerTeam },
        { "weaponDefID", weaponDefID },
        { "unit_x", kx },
        { "unit_y", ky },
        { "unit_z", kz },
        { "attacker_x", ax },
        { "attacker_y", ay },
        { "attacker_z", az }
    })

end

function widget:UnitGiven(unitID, unitDefID, newTeamID, teamID)
    local x, y, z = Spring.GetUnitPosition(unitID)

    writeJson("unit_given", {
        { "unitID", unitID },
        { "teamID", teamID },
        { "newTeamID", newTeamID },
        { "defID", unitDefID },
        { "defName", UNIT_DEF_NAMES[unitDefID] },
        { "unitX", x },
        { "unitY", y },
        { "unitZ", z },
    })
end

function widget:UnitTaken(unitID, unitDefID, oldTeamID, teamID)
    local x, y, z = Spring.GetUnitPosition(unitID)

    writeJson("unit_taken", {
        { "unitID", unitID },
        { "teamID", teamID },
        { "newTeamID", newTeamID },
        { "defID", unitDefID },
        { "defName", UNIT_DEF_NAMES[unitDefID] },
        { "unitX", x },
        { "unitY", y },
        { "unitZ", z },
    })
end

function widget:Shutdown()
    Spring.Echo("done!")
    writeJson("shutdown", {
        { "done", "done" }
    })
end
