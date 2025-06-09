using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Internal {

    [DapperColumnsMapped]
    public class AppGroup {

        public static readonly long Admin = 1;

        /// <summary>
        ///     ID of the group
        /// </summary>
        [ColumnMapping("id")]
        public long ID { get; set; }

        /// <summary>
        ///     name of the group
        /// </summary>
        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        /// <summary>
        ///     hex color without the leading #
        /// </summary>
        [ColumnMapping("hex_color")]
        public string HexColor { get; set; } = "";

    }
}
