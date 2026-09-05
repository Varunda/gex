namespace gex.Common.Models.Match {

    public class MatchSearchKeyValue {

        public string Key { get; set; } = "";

        public string Value { get; set; } = "";

        /// <summary>
        ///     valid operations are: 'eq', 'ne'
        /// </summary>
        public string Operation { get; set; } = "eq";

    }
}
