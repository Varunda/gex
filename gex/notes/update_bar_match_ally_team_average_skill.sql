-------------------------------------------------------------------------------
-- db: MAIN
-------------------------------------------------------------------------------
-- 
-- used to update the average_skill. requires patch 67 (heh) to have this column
-- 

UPDATE bar_match_ally_team
	SET average_skill = sq.avg
FROM (
	SELECT 
		at1.game_id, at1.ally_team_id, avg(skill)
	FROM 
		bar_match_ally_team at1
		INNER JOIN bar_match_player mp1 ON mp1.game_id = at1.game_id AND mp1.ally_team_id = at1.ally_team_id
	GROUP BY 
		at1.game_id, at1.ally_team_id
) sq
WHERE 
	sq.game_id = bar_match_ally_team.game_id 
	AND sq.ally_team_id = bar_match_ally_team.ally_team_id;
