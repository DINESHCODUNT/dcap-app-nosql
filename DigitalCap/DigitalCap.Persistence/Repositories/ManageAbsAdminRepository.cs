using Dapper;
using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DigitalCap.Persistence.Repositories
{
    public class ManageAbsAdminRepository : IManageAbsAdminRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;

        public ManageAbsAdminRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        public async Task<UserAccountModel?> GetByAspNetIdAsync(string aspNetUserId)
        {
            return await Connection.QuerySingleOrDefaultAsync<UserAccountModel>(
                sql: "[dbo].[sp_UserAccounts_GetByAspNetId]",
                param: new { aspNetUserId },
                transaction: Transaction,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
