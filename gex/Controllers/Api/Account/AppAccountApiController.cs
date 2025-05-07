using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using gex.Code;
using gex.Models;
using gex.Services;
using gex.Services.Repositories;
using gex.Services.Db.Account;
using gex.Models.Internal;

namespace gex.Controllers.Api.Account {

	[ApiController]
	[Route("/api/account")]
	public class AppAccountApiController : ApiControllerBase {

		private readonly ILogger<AppAccountApiController> _Logger;
		private readonly AppCurrentAccount _CurrentUser;

		private readonly AppAccountDbStore _AccountDb;

		public AppAccountApiController(ILogger<AppAccountApiController> logger, AppCurrentAccount currentUser,
			AppAccountDbStore accountDb) {

			_Logger = logger;
			_CurrentUser = currentUser;

			_AccountDb = accountDb;
		}

		/// <summary>
		///     Get the current user who is making the API call
		/// </summary>
		/// <response code="200">
		///     The response will contain the <see cref="AppAccount"/> of the user who made the API call
		/// </response>
		/// <response code="204">
		///     The user making the API call is either not signed in, or no has no account
		/// </response>
		[HttpGet("whoami")]
		public async Task<ApiResponse<AppAccount>> WhoAmI() {
			AppAccount? currentUser = await _CurrentUser.Get();

			if (currentUser == null) {
				return ApiNoContent<AppAccount>();
			}

			return ApiOk(currentUser);
		}

		/// <summary>
		///     Get all app accounts
		/// </summary>
		/// <response code="200">
		///     A list of all <see cref="AppAccount"/>s
		/// </response>
		[HttpGet]
		[Authorize]
		[PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
		public async Task<ApiResponse<List<AppAccount>>> GetAll() {
			List<AppAccount> accounts = await _AccountDb.GetAll(CancellationToken.None);

			return ApiOk(accounts);
		}

		/// <summary>
		///     Create an app account
		/// </summary>
		/// <param name="name"></param>
		/// <param name="discordID"></param>
		/// <response code="200">
		///     The <see cref="AppAccount.ID"/> of the <see cref="AppAccount"/> that was created using the parameters passed
		/// </response>
		/// <response code="400">
		///     One of the following validation errors occured:
		///     <ul>
		///         <li><paramref name="name"/> was empty or whitespace</li>
		///         <li><paramref name="discordID"/> was 0</li>
		///         <li><paramref name="discordID"/> already has an account</li>
		///     </ul>
		/// </response>
		[HttpPost("create")]
		[Authorize]
		[PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
		public async Task<ApiResponse<long>> CreateAccount([FromQuery] string name, [FromQuery] ulong discordID) {
			List<string> errors = [];

			if (string.IsNullOrWhiteSpace(name)) { errors.Add($"Missing {nameof(name)}"); }
			if (discordID == 0) { errors.Add($"Missing {nameof(discordID)}"); }

			if (errors.Count > 0) {
				return ApiBadRequest<long>($"Validation errors: {string.Join("\n", errors)}");
			}

			AppAccount? existingAccount = await _AccountDb.GetByDiscordID(discordID);
			if (existingAccount != null) {
				return ApiBadRequest<long>($"Account for discord ID {discordID} already exists");
			}

			AppAccount acc = new();
			acc.Name = name;
			acc.DiscordID = discordID;
			acc.Timestamp = DateTime.UtcNow;

			long ID = await _AccountDb.Insert(acc, CancellationToken.None);

			return ApiOk(ID);
		}

		[HttpDelete("{accountID}")]
		[Authorize]
		[PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
		public async Task<ApiResponse> DeactiviateAccount(long accountID) {
			if (accountID == 1) {
				return ApiBadRequest($"Cannot deactivate account ID 1, which is the system account");
			}

			AppAccount? currentUser = await _CurrentUser.Get();
			if (currentUser == null) {
				return ApiInternalError(new Exception($"current account is null?"));
			}

			await _AccountDb.Delete(accountID, currentUser.ID, CancellationToken.None);

			return ApiOk();
		}


	}
}
