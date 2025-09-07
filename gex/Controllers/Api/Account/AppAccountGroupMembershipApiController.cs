using gex.Code;
using gex.Models;
using gex.Models.Internal;
using gex.Services;
using gex.Services.Db.Account;
using gex.Services.Repositories.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api.Account {

    [Route("/api/group-membership")]
    [ApiController]
    public class AppAccountGroupMembershipApiController : ApiControllerBase {

        private readonly ILogger<AppAccountGroupMembershipApiController> _Logger;
        private readonly AppCurrentAccount _CurrentUser;
        private readonly AppAccountGroupMembershipRepository _MembershipRepository;
        private readonly AppAccountDbStore _AccountDb;
        private readonly AppGroupRepository _GroupRepository;

        public AppAccountGroupMembershipApiController(ILogger<AppAccountGroupMembershipApiController> logger,
            AppAccountGroupMembershipRepository membershipRepository, AppAccountDbStore accountDb,
            AppGroupRepository groupRepository, AppCurrentAccount currentUser) {

            _Logger = logger;

            _MembershipRepository = membershipRepository;
            _AccountDb = accountDb;
            _GroupRepository = groupRepository;
            _CurrentUser = currentUser;
        }

        /// <summary>
        ///		get the groups of an account
        /// </summary>
        /// <param name="accountID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpGet("account/{accountID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse<List<AppAccountGroupMembership>>> GetByAccountID(long accountID,
            CancellationToken cancel) {

            List<AppAccountGroupMembership> membership = await _MembershipRepository.GetByAccountID(accountID, cancel);

            return ApiOk(membership);
        }

        /// <summary>
        ///		get the accounts of a group
        /// </summary>
        /// <param name="groupID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpGet("group/{groupID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse<List<AppAccountGroupMembership>>> GetByGroupID(long groupID,
            CancellationToken cancel) {

            List<AppAccountGroupMembership> membership = await _MembershipRepository.GetByGroupID(groupID, cancel);

            return ApiOk(membership);
        }

        /// <summary>
        ///		add an account to a group
        /// </summary>
        /// <param name="groupID"></param>
        /// <param name="accountID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpPost("{groupID}/{accountID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse> AddUser(long groupID, long accountID,
            CancellationToken cancel) {

            AppAccount? currentUser = await _CurrentUser.Get();
            if (currentUser == null) {
                return ApiAuthorize();
            }

            _Logger.LogInformation($"user is being added to a group [groupID={groupID}] [accountID={accountID}] [currentUser={currentUser.ID}/{currentUser.Name}]");

            AppGroup? group = await _GroupRepository.GetByID(groupID, cancel);
            if (group == null) {
                return ApiNotFound($"{nameof(AppGroup)} {groupID}");
            }

            AppAccount? account = await _AccountDb.GetByID(accountID, cancel);
            if (account == null) {
                return ApiNotFound($"{nameof(AppAccount)} {accountID}");
            }

            AppAccountGroupMembership membership = new();
            membership.AccountID = accountID;
            membership.GroupID = groupID;
            membership.GrantedByAccountID = currentUser.ID;
            membership.Timestamp = DateTime.UtcNow;

            membership.ID = await _MembershipRepository.Insert(membership, cancel);

            return ApiOk();
        }

        /// <summary>
        ///		remove an account from a <see cref="AppGroup"/>
        /// </summary>
        /// <param name="groupID">ID of the group to remove the user from</param>
        /// <param name="accountID">ID of the account to be removed from the group</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpDelete("{groupID}/{accountID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse> RemoveUser(long groupID, long accountID,
            CancellationToken cancel) {

            AppAccount? currentUser = await _CurrentUser.Get();
            if (currentUser == null) {
                return ApiAuthorize();
            }

            _Logger.LogInformation($"user is being removed from group [groupID={groupID}] [accountID={accountID}] [currentUser={currentUser.ID}/{currentUser.Name}]");

            List<AppAccountGroupMembership> memberships = await _MembershipRepository.GetByAccountID(accountID, cancel);
            AppAccountGroupMembership? membership = memberships.FirstOrDefault(iter => iter.GroupID == groupID);

            if (membership == null) {
                return ApiBadRequest($"account {accountID} is not a member of group {groupID}");
            }

            await _MembershipRepository.Delete(membership, cancel);

            return ApiOk();
        }

    }

}
