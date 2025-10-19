namespace gex.Models.Db {

    public class SearchKeyValue {

        public string Key { get; set; } = "";

        public string Value { get; set; } = "";

        /// <summary>
        ///     valid operations are: 'eq', 'ne'
        /// </summary>
        public string Operation { get; set; } = "eq";

    }
}
