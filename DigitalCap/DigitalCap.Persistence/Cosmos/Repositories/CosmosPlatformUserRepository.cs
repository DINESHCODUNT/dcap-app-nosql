using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    /// <summary>
    /// Replaces PlatformUserRepository. The SQL version has no constructor (its fields are never set, so every method fails)
    /// and no screen calls it (PlatformUserService is injected but never used). Its 3 stored procedures do not exist in SQL.
    /// </summary>
    public class CosmosPlatformUserRepository : IPlatformUserRepository
    {
        public Task<IEnumerable<UserType>> GetUserType(string objectId) =>
            throw new NotSupportedException("PlatformUserRepository.GetUserType is not used by the app and is not converted to Cosmos.");

        public Task<IEnumerable<UserWCNMappingModel>> GetWCNGridAsync(string email) =>
            throw new NotSupportedException("Stored procedure not present in source database: CAP.Get_WCN_Grid");

        public Task<string?> GetTermsOfUseTextAsync(int applicationId) =>
            throw new NotSupportedException("Stored procedure not present in source database: CAP.Get_Terms_Of_Use_Text");

        public Task<bool> AddNewWCNAsync(string email, WCNModel model) =>
            throw new NotSupportedException("Stored procedure not present in source database: CAP.Add_New_WCN");
    }
}
