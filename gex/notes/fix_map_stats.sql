
-- Query used to fix user map stats
-- needs patch 36 to run, else it'll error as before then the gamemode was part of the pkey

insert into bar_user_map_stats (user_id, map, gamemode, play_count, win_count, loss_count, tie_count, last_updated)
	select
		p.user_id,
		m.map,
		m.gamemode,
		count(*) filter (where at.ally_team_id = p.ally_team_id) "play_count",
		count(*) filter (where at.won = true and at.ally_team_id = p.ally_team_id) "win_count",
		count(*) filter (where at.won = false and at.ally_team_id = p.ally_team_id) "loss_count",
		0 "tie_count",
		now() at time zone 'utc' "last_updated"
	FROM 
		bar_match m
		LEFT JOIN bar_match_player p ON m.id = p.game_id
		LEFT JOIN bar_match_ally_team at ON m.id = at.game_id
	WHERE
		m.gamemode <> 0
	GROUP BY
		p.user_id,
		m.map,
		m.gamemode
ON CONFLICT (user_id, map, gamemode) DO UPDATE 
	SET play_count = excluded.play_count,
		win_count = excluded.win_count,
		loss_count = excluded.loss_count,
		last_updated = excluded.last_updated;
