using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Internal {

    [DapperColumnsMapped]
    public class AppGroupPermission {

        /// <summary>
        ///     Unique ID of the permission
        /// </summary>
        [ColumnMapping("id")]
        public long ID { get; set; }

        /// <summary>
        ///     What <see cref="AppGroup"/> this permission is granted to
        /// </summary>
        [ColumnMapping("group_id")]
        public long GroupID { get; set; }

        /// <summary>
        ///     What the permission is
        /// </summary>
        [ColumnMapping("permission")]
        public string Permission { get; set; } = "";

        /// <summary>
        ///     When this permission was added
        /// </summary>
        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     What <see cref="AppAccount"/> granted this permission
        /// </summary>
        [ColumnMapping("granted_by_id")]
        public long GrantedByID { get; set; }

    }
}
