using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Internal {

	[DapperColumnsMapped]
	public class AppAccountGroupMembership {

		/// <summary>
		///     unique ID
		/// </summary>
		[ColumnMapping("id")]
        public long ID { get; set; }

        /// <summary>
        ///     ID of the <see cref="AppAccount"/> this membership is for
        /// </summary>
		[ColumnMapping("account_id")]
        public long AccountID { get; set; }

        /// <summary>
        ///     ID of the <see cref="AppGroup"/> this membership is for
        /// </summary>
		[ColumnMapping("group_id")]
        public long GroupID { get; set; }

        /// <summary>
        ///     when this entry was created
        /// </summary>
		[ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     who granted the user this permission
        /// </summary>
		[ColumnMapping("granted_by_account_id")]
        public long GrantedByAccountID { get; set; }

	}
}
