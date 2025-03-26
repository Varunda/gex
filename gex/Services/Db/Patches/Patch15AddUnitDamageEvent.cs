﻿using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch15AddUnitDamageEvent : IDbPatch {
        public int MinVersion => 15;
        public string Name => "add game_event_unit_damage";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS game_event_unit_damage (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    team_id int NOT NULL,
                    definition_id int NOT NULL,
                    damage_dealt double precision NOT NULL,
                    damage_taken double precision NOT NULL,

                    CONSTRAINT unq_unit_damage_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_unit_damage ON game_event_unit_damage (game_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
