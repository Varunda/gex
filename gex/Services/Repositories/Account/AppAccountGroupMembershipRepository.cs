using gex.Models.Internal;
using gex.Services.Db.Account;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories.Account {

	public class AppAccountGroupMembershipRepository {

		private readonly ILogger<AppAccountGroupMembershipRepository> _Logger;
		private readonly AppAccountGroupMembershipDb _MembershipDb;

		private readonly IMemoryCache _Cache;

		private const string CACHE_KEY_ACCOUNT = "App.AccountGroupMembership.Account.{0}"; // {0} => account ID

		public AppAccountGroupMembershipRepository(ILogger<AppAccountGroupMembershipRepository> logger,
			AppAccountGroupMembershipDb membershipDb, IMemoryCache cache) {

			_Logger = logger;
			_MembershipDb = membershipDb;
			_Cache = cache;
		}

		public Task<AppAccountGroupMembership?> GetByID(long ID, CancellationToken cancel) {
			return _MembershipDb.GetByID(ID, cancel);
		}

		public Task<List<AppAccountGroupMembership>> GetByAccountID(long accountID, CancellationToken cancel) {
			return _MembershipDb.GetByAccountID(accountID, cancel);
		}

		public Task<List<AppAccountGroupMembership>> GetByGroupID(long groupID, CancellationToken cancel) {
			return _MembershipDb.GetByGroupID(groupID, cancel);
		}

		public Task<long> Insert(AppAccountGroupMembership membership, CancellationToken cancel) {
			return _MembershipDb.Insert(membership, cancel);
		}

		public Task Delete(AppAccountGroupMembership membership, CancellationToken cancel) {
			return _MembershipDb.Delete(membership, cancel);
		}

	}

}
