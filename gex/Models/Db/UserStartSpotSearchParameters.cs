using System;

namespace gex.Models.Db {

    public class UserStartSpotSearchParameters {

        public long UserID { get; set; }

        public string MapFilename { get; set; } = "";

        public byte? Gamemode { get; set; }

        public float? MinimumOS { get; set; }

        public float? MinimumAverageOS { get; set; }

        public float? MaximumOS { get; set; }

        public float? MaximumAverageOS { get; set; }

        public DateTime? PeriodStart { get; set; }

        public DateTime? PeriodEnd { get; set; }

        public string GetCacheKey() {
            return $"{UserID}.{MapFilename}"
                + $".gm{Gamemode}.minos{MinimumOS}.minaos{MinimumAverageOS}.maxos{MaximumOS}.maxaos{MaximumAverageOS}"
                + $".ps{PeriodStart?.ToString("u")}.pe{PeriodEnd?.ToString("u")}";
        }

    }
}
