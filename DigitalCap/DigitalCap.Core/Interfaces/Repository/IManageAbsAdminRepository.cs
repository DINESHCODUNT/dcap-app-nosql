using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IManageAbsAdminRepository
    {
        Task<UserAccountModel?> GetByAspNetIdAsync(string aspNetUserId);
    }
}
