using DigitalCap.Core.DTO;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IManageAbsAdminService

    {
        Task<ServiceResult<AccountViewModel>> GetAbsUserAsync(Guid userId);
        Task<ServiceResult<bool>> StoreAbsUserAsync(AccountViewModel model);
    }
}
