using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IPlatformUserService
    {
        Task<List<UserWCNMappingModel>> GetWCNGrid();

        Task<string> GetTermsOfUse(int applicationId);

        Task<IEnumerable<UserType>> GetUserType();

        Task<bool> AddNewWCNAsync(WCNModel wCNModel);
    }
}
