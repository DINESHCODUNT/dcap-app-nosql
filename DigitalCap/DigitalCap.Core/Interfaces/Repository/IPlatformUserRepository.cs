using DigitalCap.Core.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DigitalCap.Core.Models;

namespace DigitalCap.Core.Interfaces.Repository
{

    public interface IPlatformUserRepository
    {
        Task<IEnumerable<UserType>> GetUserType(string objectId);
        Task<IEnumerable<UserWCNMappingModel>> GetWCNGridAsync(string email);
        Task<string?> GetTermsOfUseTextAsync(int applicationId);
        Task<bool> AddNewWCNAsync(string email, WCNModel model);
    }
}






