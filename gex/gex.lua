local headless = (Spring.GetConfigInt("Headless", 0) ~= 0) 
Spring.Echo("[Gex] started gex!", headless)

local UNIT_DEF_NAMES = {}
local UNIT_DEF_METAL = {}
local UNIT_DEF_IS_COMMANDER = {}
local UNIT_DEF_BUILD_POWER = {}
local UNIT_DEF_HEALTH = {}
local frame = 0
local timer
local commanders = {}

local units_died_in_frame = {}
local units_pending_load = {}
local units_pending_unload = {}

local CMD_RECLAIM = 90
local CMD_RESTORE = 110
local CMD_RESURRECT = 125
local CMD_CAPTURE = 130

-- apm calculations are copied from
-- copied from https://github.com/beyond-all-reason/Beyond-All-Reason/blob/master/luarules/gadgets/game_apm_broadcast.lua
local teamAddedActionFrame = {}
local ignoreUnits = {}
local spGetUnitIsBeingBuilt = Spring.GetUnitIsBeingBuilt

local totalTeamActions = {}
for _, teamID in ipairs(Spring.GetTeamList()) do
	totalTeamActions[teamID] = 0
end
local ignoreUnitDefs = {}
for uDefID, uDef in pairs(UnitDefs) do
	if uDef.customParams.drone then
		ignoreUnitDefs[uDefID] = true
	end
end

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

local UNIT_RESOURCE_PRODUCTION = {}
local UNIT_DAMAGE = {}

local UNIT_TYPE_ARMY = {}
local UNIT_TYPE_DEF = {}
local UNIT_TYPE_UTIL = {}
local UNIT_TYPE_ECO = {}

local lastFrameUpdate = 0

local function SendExtraStats()

    local teamApms = {}
	for teamID, totalActions in pairs(totalTeamActions) do
		if (teamID ~= Spring.GetGaiaTeamID()) then
            teamApms[teamID] = totalActions
        end
	end

    lastFrameUpdate = frame

	local teamList = Spring.GetTeamList()
	for _,teamID in ipairs(teamList) do
		if (teamID ~= Spring.GetGaiaTeamID()) then
			local units = Spring.GetTeamUnits(teamID)
			local total_metal_cost = 0

            local armyValue = 0
            local defValue = 0
            local utilValue = 0
            local ecoValue = 0
            local otherValue = 0

            local metalCurrent, metalStorage, metalPull, metalIncome, metalExpense, metalShare, metalSent, metalReceived = Spring.GetTeamResources(teamID, "metal")
			local energyCurrent, energyStorage, energyPull, energyIncome, energyExpense, energyShare, energySent, energyReceived = Spring.GetTeamResources(teamID, "energy")

			local total_bp = 0
			local used_bp = 0

			for k,v in pairs(units) do
				local uid = Spring.GetUnitDefID(v)

                if (UNIT_TYPE_ARMY[uid] ~= nil) then
                    armyValue = armyValue + UNIT_TYPE_ARMY[uid]
                elseif (UNIT_TYPE_DEF[uid] ~= nil) then
                    defValue = defValue + UNIT_TYPE_DEF[uid]
                elseif (UNIT_TYPE_UTIL[uid] ~= nil) then
                    utilValue = utilValue + UNIT_TYPE_UTIL[uid]
                elseif (UNIT_TYPE_ECO[uid] ~= nil) then
                    ecoValue = ecoValue + UNIT_TYPE_ECO[uid]
                else
                    otherValue = otherValue + (UNIT_DEF_METAL[uid] or 0)
                end

				total_metal_cost = total_metal_cost + (UNIT_DEF_METAL[uid] or 0)

				if (UNIT_DEF_BUILD_POWER[uid] ~= nil) then
					total_bp = total_bp + UNIT_DEF_BUILD_POWER[uid]

					-- https://github.com/beyond-all-reason/spring/blob/c657b811f48920286243a9fc5bab29e8f3251469/rts/Sim/Units/CommandAI/Command.h#L50
					local cmdID = Spring.GetUnitWorkerTask(v)
					if (cmdID ~= nil) then
						used_bp = used_bp + UNIT_DEF_BUILD_POWER[uid]
					end
				end
			end

			writeJson("extra_stat_update", {
				{ "teamID", teamID },
				{ "totalValue", total_metal_cost },
                { "armyValue", armyValue },
                { "defValue", defValue },
                { "utilValue", utilValue },
                { "metalCurrent", metalCurrent },
                { "energyCurrent", energyCurrent },
                { "ecoValue", ecoValue },
                { "otherValue", otherValue },
				{ "buildPowerAvailable", total_bp },
				{ "buildPowerUsed", used_bp },
                { "actions", teamApms[teamID] or 0 }
			})
		end
	end
end

local UNIT_POSITION = {}

local function SendUnitPositions()
    local unitIDs = Spring.GetAllUnits()
    for _,unitID in pairs(unitIDs) do
        if (Spring.GetUnitTeam(unitID) ~= Spring.GetGaiaTeamID()) then
			local x, y, z = Spring.GetUnitPosition(unitID)

            if (UNIT_POSITION[unitID] ~= nil) then
                local px = UNIT_POSITION[unitID].x
                local py = UNIT_POSITION[unitID].y
                local pz = UNIT_POSITION[unitID].z
                if (px ~= x or py ~= y or pz ~= z) then
                    writeJson("unit_position", {
                        { "unitID", unitID },
                        { "teamID", Spring.GetUnitTeam(unitID) },
                        { "x", x },
                        { "y", y }, 
                        { "z", z }
                    })
                end
            else
				writeJson("unit_position", {
					{ "unitID", unitID },
					{ "teamID", Spring.GetUnitTeam(unitID) },
					{ "x", x },
					{ "y", y }, 
					{ "z", z }
				})
            end

            UNIT_POSITION[unitID] = { x = x, y = y, z = z }
        end
    end
end

function widget:GameFrame(n)
    -- 2025-06-22 HACK:
    -- ok, this is really fucking stupid code, caused by the classic "what the fuck is a wupget"
    -- widgets don't get the GameFramePost callin, but Gex still needs to emit this data at the
    -- end of a frame, so Gex emits this data before the |frame| global is updated so the frame emitted
    -- is one frame back from what the game's frame actually is :>
    units_died_in_frame = {} -- reset back to nothing

    for unitID,v in pairs(units_pending_load) do
        writeJson("transport_loaded", v)
    end
    units_pending_load = {}

    for unitID,v in pairs(units_pending_unload) do
        writeJson("transport_unloaded", v)
    end
	units_pending_unload = {}

    frame = n

    local unitIDs = Spring.GetAllUnits()
    for _,unitID in pairs(unitIDs) do
        if (Spring.GetUnitTeam(unitID) ~= Spring.GetGaiaTeamID()) then
            local metalMake, metalUse, energyMake, energyUse = Spring.GetUnitResources(unitID)
            if (((metalMake or 0) > 0) or ((metalUse or 0) > 0) or ((energyMake or 0) > 0) or ((energyUse or 0) > 0)) then
                if (UNIT_RESOURCE_PRODUCTION[unitID] == nil) then
                    UNIT_RESOURCE_PRODUCTION[unitID] = {
                        metalMake = 0,
                        metalUse = 0,
                        energyMake = 0,
                        energyUse = 0
                    }
                end

                UNIT_RESOURCE_PRODUCTION[unitID].metalMake = UNIT_RESOURCE_PRODUCTION[unitID].metalMake + (metalMake / 30)
                UNIT_RESOURCE_PRODUCTION[unitID].metalUse = UNIT_RESOURCE_PRODUCTION[unitID].metalUse + (metalUse / 30)
                UNIT_RESOURCE_PRODUCTION[unitID].energyMake = UNIT_RESOURCE_PRODUCTION[unitID].energyMake + (energyMake / 30)
                UNIT_RESOURCE_PRODUCTION[unitID].energyUse = UNIT_RESOURCE_PRODUCTION[unitID].energyUse + (energyUse / 30)
            end
        end
    end

    -- every 15 seconds
    if (frame % 150 == 0) then
        local _, _, _, actual_thing_i_care_about, _, _, _ = Spring.GetWind()

        writeJson("wind_update", {
            { "value", actual_thing_i_care_about }
        })

        for _,unitID in pairs(commanders) do
            local posx, posy, posz = Spring.GetUnitPosition(unitID)
            -- sometimes this can be nil for reasons ??
            if (posx ~= nil and posy ~= nil and posz ~= nil) then
				writeJson("commander_position_update", {
					{ "unitID", unitID },
					{ "posX", posx, },
					{ "posY", posy, },
					{ "posZ", posz, }
				})
            end
        end
    end

	teamAddedActionFrame = {}
    -- every 15 seconds
    if (frame % 450 == 0) then
        SendExtraStats()
    end

    -- every 30 seconds
    if (frame % 900 == 0) then
        SendUnitPositions()
    end

    -- every 20 second
    if (frame % 600 == 0) then
        Spring.Echo("[Gex] on frame " .. frame)
    end

	for unitID, f in pairs(ignoreUnits) do
		if f == frame then
			ignoreUnits[unitID] = nil
		end
	end
end

function widget:TeamDied(teamID)
    writeJson("team_died", {
        { "teamID", teamID }
    })
end

function widget:GameOver(winningAllyTeams)
	Spring.Echo("[Gex] on frame " .. frame)

    local time = Spring.DiffTimers(Spring.GetTimer(), timer)

    local data = {
        { "realtime", time },
        { "ingame", Spring.GetGameSeconds() }
    }

    writeJson("end", data)

    for unitID,v in pairs(UNIT_RESOURCE_PRODUCTION) do
        -- HACK: for some reason, the UnitKilled callin setting nil does not work,
        -- and they are still iterated thru in this loop. but, GetUnitTeam will return nil for these units
        if ((Spring.GetUnitTeam(unitID) ~= nil) and (units_died_in_frame[unitID] == nil)) then
			writeJson("unit_resources", {
				{ "unitID", unitID },
                { "defID", Spring.GetUnitDefID(unitID) },
				{ "teamID", Spring.GetUnitTeam(unitID) },
				{ "metalMake", v.metalMake },
				{ "metalUse", v.metalUse },
				{ "energyMake", v.energyMake },
				{ "energyUse", v.energyUse }
			})
            UNIT_RESOURCE_PRODUCTION[unitID] = nil
        end
    end

    for unitID,v in pairs(UNIT_DAMAGE) do
        if ((Spring.GetUnitTeam(unitID) ~= nil) and (units_died_in_frame[unitID] == nil)) then
			writeJson("unit_damage", {
				{ "unitID", unitID },
				{ "defID", Spring.GetUnitDefID(unitID) },
				{ "teamID", Spring.GetUnitTeam(unitID) },
				{ "dealt", v.dealt },
				{ "taken", v.taken }
			})
            -- units can sometimes die after (or on the same frame?) the game ends,
            --      which would end this again (on the same frame), which is duplicate info
            UNIT_DAMAGE[unitID] = nil
        end
    end

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

    -- also send on last frame to match partity with history stats (which includes last frame)
    -- EXCEPT, every 450 frames, the extra stats are already sent as part of the frame, so DO not send,
    --      as they have already been sent for this frame, and it would be duplicate data!
    if (frame % 450 ~= 0) then
		SendExtraStats()
    else
        Spring.Echo("[Gex] not sending extra stats on GameOver, frame is a multiple of 450")
    end

    if (frame % 900 ~= 0) then
        SendUnitPositions()
    end

    Spring.Echo("[Gex] game over, force quitting")
    Spring.SendCommands("quitforce")
end

function widget:UnitCreated(unitID, unitDefID, teamID)
	ignoreUnits[unitID] = frame + 1

    if (UNIT_DEF_IS_COMMANDER[unitDefID] == true) then
        Spring.Echo("commander unit created", unitID, "defID", unitDefID)
		commanders[unitID] = unitID
    end

    local x, y, z = Spring.GetUnitPosition(unitID)
    local yaw = Spring.GetUnitHeading(unitID, true) -- unit: radians

    writeJson("unit_created", {
        { "unitID", unitID },
        { "teamID", teamID },
        { "defID", unitDefID },
        { "defName", UNIT_DEF_NAMES[unitDefID] },
        { "unit_x", x },
        { "unit_y", y },
        { "unit_z", z },
        { "yaw", yaw }, -- this matters for non-square units that will be displayed on map
    })
end

function widget:UnitDestroyed(unitID, unitDefID, teamID, attackerID, attackerDefID, attackerTeam, weaponDefID)

    if (commanders[unitID] ~= nil) then
        commanders[unitID] = nil
    end

    units_died_in_frame[unitID] = unitID

    -- if a unit was killed by crashing, assign the kill credit to whatever last hurt the unit
    -- this is useful for air to air kills, as those mostly end with crashes
    -- thanks to TheDujin for finding this bug and reporting it!
    if (weaponDefID == -8) then
        attackerID = Spring.GetUnitLastAttacker(unitID)
        if (attackerID ~= nil) then
            attackerDefID = Spring.GetUnitDefID(attackerID)
            attackerTeam = Spring.GetUnitTeam(attackerID)
        end
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

    if (UNIT_RESOURCE_PRODUCTION[unitID] ~= nil) then
        local v = UNIT_RESOURCE_PRODUCTION[unitID]
        writeJson("unit_resources", {
            { "unitID", unitID },
            { "defID", unitDefID },
            { "teamID", teamID },
            { "metalMake", v.metalMake },
            { "metalUse", v.metalUse },
            { "energyMake", v.energyMake },
            { "energyUse", v.energyUse }
        })
        UNIT_RESOURCE_PRODUCTION[unitID] = nil
    end

    if (UNIT_DAMAGE[unitID] ~= nil) then
		local v = UNIT_DAMAGE[unitID]
        writeJson("unit_damage", {
            { "unitID", unitID },
            { "defID", unitDefID },
            { "teamID", teamID },
            { "dealt", v.dealt },
            { "taken", v.taken }
        })
        UNIT_DAMAGE[unitID] = nil
    end
end

function widget:UnitLoaded(unitID, unitDefID, unitTeam, transportID, transportTeam)

    -- 2025-06-21 HACK:
    -- a unit can be loaded and unloaded by different transports multiple times per frame.
    -- instead of sending all of those events, send all of the unit loaded events after the frame is over
    -- ex:
    -- {"action":"transport_loaded","frame":47620,"unitID":25692,"defID":452,"teamID":0,"transportUnitID":10061,"transportTeamID":0,...}
	-- {"action":"transport_unloaded","frame":47620,"unitID":25692,"defID":452,"teamID":0,"transportUnitID":10061,"transportTeamID":0,...}
	-- {"action":"transport_loaded","frame":47620,"unitID":25692,"defID":452,"teamID":0,"transportUnitID":24424,"transportTeamID":0,...}
	-- {"action":"transport_unloaded","frame":47620,"unitID":25692,"defID":452,"teamID":0,"transportUnitID":24424,"transportTeamID":0,...}
	-- {"action":"transport_loaded","frame":47620,"unitID":25692,"defID":452,"teamID":0,"transportUnitID":29322,"transportTeamID":0,...}
	-- {"action":"transport_unloaded","frame":47620,"unitID":25692,"defID":452,"teamID":0,"transportUnitID":29322,"transportTeamID":0,...}
    --
    -- note the unitID is the same for all events, but the transport ID is different
    -- only emit the last one of these events per frame

    local x, y, z = Spring.GetUnitPosition(unitID)

    units_pending_load[unitID] = {
        { "unitID", unitID },
        { "defID", unitDefID },
        { "teamID", unitTeam },
        { "transportUnitID", transportID },
        { "transportTeamID", transportTeam },
        { "unitX", x },
        { "unitY", y },
        { "unitZ", z }
    }

    --[[
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
    ]]
end

function widget:UnitUnloaded(unitID, unitDefID, unitTeam, transportID, transportTeam)

    local x, y, z = Spring.GetUnitPosition(unitID)

    units_pending_unload[unitID] = {
        { "unitID", unitID },
        { "defID", unitDefID },
        { "teamID", unitTeam },
        { "transportUnitID", transportID },
        { "transportTeamID", transportTeam },
        { "unitX", x },
        { "unitY", y },
        { "unitZ", z }
    }

    --[[
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
    ]]
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

local function UnitDamagedReplay(unitID, unitDefID, teamID, damage, paralyzer, weaponDefID, projectileID, attackerID, attackerDefID, attackerTeam)
    -- TODO: this prevents self-d damage from counting right?
    if (unitID == attackerID) then
		return
    end

	if (UNIT_DAMAGE[unitID] == nil) then
		UNIT_DAMAGE[unitID] = {
            unitID = unitID,
            teamID = teamID,
            taken = 0,
            dealt = 0
		}
	end

    -- cap damage to the max health of a unit, otherwise things like a D-gun give a lot of efficiency
    local unitMaxHealth = UNIT_DEF_HEALTH[unitDefID] or damage
    if (damage > unitMaxHealth) then
        damage = unitMaxHealth
    end

    UNIT_DAMAGE[unitID].taken = UNIT_DAMAGE[unitID].taken + damage

    if (attackerID ~= nil) then
		if (UNIT_DAMAGE[attackerID] == nil) then
			UNIT_DAMAGE[attackerID] = {
				unitID = attackerID,
				teamID = teamID,
                taken = 0,
                dealt = 0
			}
		end

		UNIT_DAMAGE[attackerID].dealt = UNIT_DAMAGE[attackerID].dealt + damage
    end
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

-- be aware that these arent exclusively user actioned commands
function widget:UnitCommand(unitID, unitDefID, teamID, cmdID, cmdParams, cmdOptions, cmdTag)
	-- limit to 1 action per gameframe
	if not teamAddedActionFrame[teamID] and totalTeamActions[teamID] and not ignoreUnitDefs[unitID] then
		if not ignoreUnits[unitID] and not spGetUnitIsBeingBuilt(unitID) then	-- believe it or not but unitcreated can come after AllowCommand (with nocost at least)
			totalTeamActions[teamID] = totalTeamActions[teamID] + 1
			teamAddedActionFrame[teamID] = true
		end
	end
	ignoreUnits[unitID] = frame + 7	-- dont count severe cmd spam
end

function widget:Initialize()
    Spring.Echo("[Gex] starting game event extractor (gex)")

    local headless = (Spring.GetConfigInt("Headless", 0) ~= 0)
    if (not headless) then
        Spring.Echo("[Gex] not running gex, Headless is", headless)
        return
    end

    -- usually, UnitDamaged doesn't contain the attacker ID, but only in a replay or spectating, a global
    -- that we can register is allowed that gives us the attacker ID
    -- https://github.com/beyond-all-reason/Beyond-All-Reason/blob/fa2cd41dd049ac9cda9422e4860a7b2d8e39efa6/luarules/gadgets/dev_replay_data.lua#L3
    widgetHandler:RegisterGlobal("UnitDamagedReplay", UnitDamagedReplay);

    file = io.open("actions.json", "w")
    io.output(file)
    io.write("{\"action\":\"init\",\"frame\":0,\"version\":1}\n")

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
            { "sizeX", v.xsize },
            { "sizeZ", v.zsize },

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
            { "radarDistance", v.radarDistance },
            { "attackRange", v.maxWeaponRange },
            { "isCommander", v.customParams.iscommander ~= nil },
            { "isReclaimer", v.isBuilder and not v.isFactory },
            { "isFactory", v.isFactory },
            { "weaponCount", #v.weapons }
        })

        UNIT_DEF_NAMES[k] = v["name"]
        UNIT_DEF_METAL[k] = v["metalCost"]
        UNIT_DEF_IS_COMMANDER[k] = v.customParams.iscommander ~= nil
        UNIT_DEF_HEALTH[k] = v.health

        if ((not v.customParams.iscommander) and (v.weapons and #v.weapons > 0) and (v.speed and v.speed > 0)) then
            UNIT_TYPE_ARMY[k] = v.metalCost
        elseif ((v.weapons and #v.weapons > 0) and (not v.speed or (v.speed == 0))) then
            UNIT_TYPE_DEF[k] = v.metalCost
        elseif (v.customParams.unitgroup == 'util') then
            UNIT_TYPE_UTIL[k] = v.metalCost
        elseif (v.customParams.unitgroup == 'metal' or v.customParams.unitgroup == 'energy') then
            UNIT_TYPE_ECO[k] = v.metalCost
        end

        if (v.buildSpeed > 0) then
			UNIT_DEF_BUILD_POWER[k] = v.buildSpeed
        end
	end
    io.flush()
    io.close(file)

    Spring.SendCommands(
        "setmaxspeed 9999", "setminspeed 9999", "hideinterface"
    )

    Spring.SendCommands("skip 1")
end

function widget:Shutdown()
    Spring.Echo("done!")
    writeJson("shutdown", {
        { "done", "done" }
    })

    widgetHandler:DeregisterGlobal("UnitDamagedReplay");
end
