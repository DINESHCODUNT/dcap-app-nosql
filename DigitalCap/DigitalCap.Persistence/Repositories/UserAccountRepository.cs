using Dapper;
using DigitalCap.Core.DTO;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Transactions;

namespace DigitalCap.Persistence.Repositories
{
    public class UserAccountRepository : RepositoryBase<UserAccountModel, Guid>, IUserAccountRepository
    {

        private readonly ILogger<UserAccountRepository> _logger;
        public UserAccountRepository(IUnitOfWork unitOfWork, ILogger<UserAccountRepository> logger)
       : base(unitOfWork)
        {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserAccountModel> GetByAspNetId(string id)
        {
            var result = await Connection.QuerySingleOrDefaultAsync<UserAccountModel>(
                sql: "sp_UserAccounts_GetByAspNetId",
                commandType: CommandType.StoredProcedure,
                param: new { id },
                transaction: Transaction);

            return result;
        }

        public async Task<UserAccountModel?> GetByAspNetIdAsync(string aspNetUserId)
        {
            return await Connection.QuerySingleOrDefaultAsync<UserAccountModel>(
                sql: "sp_UserAccounts_GetByAspNetId",
                param: new { Id = aspNetUserId },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
        }

        public async Task<UserAccountModel?> GetByAspNetIdIncludingDeletedAsync(string id)
        {
            return await Connection.QuerySingleOrDefaultAsync<UserAccountModel>(
                sql: "sp_UserAccounts_GetByAspNetId_ActiveOrDeleted",
                param: new { id },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
        }


        public async Task<IEnumerable<UserAccountModel>> GetUsersInRoleAsync(string roleName)
        {
            return await Connection.QueryAsync<UserAccountModel>(
                sql: "sp_UserAccounts_GetUsersInRole",
                param: new { roleName },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
        }

        public async Task<IEnumerable<UserAccountModel>> GetUsersInRoleForClientAsync(
            string roleName, Guid clientId)
        {
            return await Connection.QueryAsync<UserAccountModel>(
                sql: "sp_UserAccounts_GetUsersInRoleForClient",
                param: new { roleName, clientId },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
        }

        public async Task<IEnumerable<UserAccountModel>> GetUsersWithPermissionAsync(Permission permission)
        {
            return await Connection.QueryAsync<UserAccountModel>(
                sql: "sp_UserAccounts_GetUsersWithPermission",
                param: new { permissionName = permission.ToString() },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
        }

        public async Task<IEnumerable<CapUser>> GetAbsUsersAsync()
        {
            return await Connection.QueryAsync<CapUser>(
                sql: "sp_UserAccounts_GetAbsUsers",
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
        }

        public async Task<IEnumerable<CapUser>> GetClientUsersAsync(string clientId)
        {
            return await Connection.QueryAsync<CapUser>(
                sql: "sp_UserAccounts_GetClientUsers",
                param: new { clientId },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
        }

        //Manage AbsUser
        public async Task<IEnumerable<UserAccountModel>> GetAbsUsers()
        {
            return await Connection.QueryAsync<UserAccountModel>(
                "sp_UserAccounts_GetAbsUsers",
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
        }

        public async Task<UserAccountModel> GetByAspNetId_ActiveOrDeleted(string id)
        {

            var result = await Connection.QuerySingleOrDefaultAsync<UserAccountModel>(
                sql: "sp_UserAccounts_GetByAspNetId_ActiveOrDeleted",
                commandType: CommandType.StoredProcedure,
                param: new { id },
                transaction: Transaction);

            return result;

        }

        public async Task<List<AccountDto>> GetClientAdminsAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            try
            {
                const string sql = @"
                    SELECT 
                        u.Id,
                        ua.FirstName,
                        ua.LastName,
                        u.Email,
                        ua.Phone,
                        ua.TimeZone,
                        ua.ClientId
                    FROM [dbo].[AspNetUsers] u
                    INNER JOIN [dbo].[UserAccounts] ua ON u.Id = ua.AspNetUserId
                    WHERE ua.ClientId = @ClientId
                    ORDER BY ua.LastName, ua.FirstName";
                var result = await Connection.QueryAsync<AccountDto>(
                    sql: sql,
                    param: new { ClientId = clientId },
                    transaction: Transaction
                );

                _logger.LogInformation($"Retrieved {result.Count()} admins for client {clientId}");

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving client admins for client {clientId}");
                throw;
            }
        }


    }
}

