using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using System;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosManageAbsAdminRepository : IManageAbsAdminRepository
    {
        public Task<UserAccountModel?> GetByAspNetIdAsync(string aspNetUserId)
        {
            throw new NotSupportedException("ManageAbsAdminRepository.GetByAspNetIdAsync is not used by the app and is not converted to Cosmos.");
        }
    }
}
