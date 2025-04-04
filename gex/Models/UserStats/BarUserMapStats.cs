﻿using Dapper.ColumnMapper;
using gex.Code;
using gex.Code.Constants;
using System;

namespace gex.Models.UserStats {

    [DapperColumnsMapped]
    public class BarUserMapStats {

        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        [ColumnMapping("map")]
        public string Map { get; set; } = "";

        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        [ColumnMapping("play_count")]
        public int PlayCount { get; set; }

        [ColumnMapping("win_count")]
        public int WinCount { get; set; }

        [ColumnMapping("loss_count")]
        public int LossCount { get; set; }

        [ColumnMapping("tie_count")]
        public int TieCount { get; set; }

        [ColumnMapping("last_updated")]
        public DateTime LastUpdated { get; set; }

    }
}
