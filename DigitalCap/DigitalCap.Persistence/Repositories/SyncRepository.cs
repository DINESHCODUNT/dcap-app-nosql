using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models.Survey;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class SyncRepository : ISyncRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;
        public async Task<LoginData?> GetUserInfo()
        {
            return await Connection.QueryFirstOrDefaultAsync<LoginData>(
                sql: @"SELECT 
                    Id,
                    FirstName,
                    LastName,
                    ProviderKey,
                    EmailId
               FROM LogInData",
                commandType: CommandType.Text,
                transaction: Transaction
            );
        }

    }
}
