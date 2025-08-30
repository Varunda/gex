namespace gex.Models.UserStats {

    public class BarUserInteractions {

        /// <summary>
        ///     source user
        /// </summary>
        public long UserID { get; set; }

        /// <summary>
        ///     ID of the user the counts are for
        /// </summary>
        public long TargetUserID { get; set; }

        /// <summary>
        ///     how many games <see cref="UserID"/> has played on the same ally team as <see cref="TargetUserID"/>
        /// </summary>
        public int WithCount { get; set; }

        /// <summary>
        ///     how many games <see cref="UserID"/> has won on the same ally team as <see cref="TargetUserID"/>
        /// </summary>
        public int WithWin { get; set; }

        /// <summary>
        ///     how many games <see cref="UserID"/> has played on a different ally team as <see cref="TargetUserID"/>
        /// </summary>
        public int AgainstCount { get; set; }

        /// <summary>
        ///     how many games <see cref="UserID"/> has won on a different ally team as <see cref="TargetUserID"/>
        /// </summary>
        public int AgainstWin { get; set; }

    }
}
