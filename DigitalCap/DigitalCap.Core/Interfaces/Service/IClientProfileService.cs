using DigitalCap.Core.DTO;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static DigitalCap.Core.DTO.ClientProfileDTO;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IClientProfileService
    {
        Task<ServiceResult<List<ClientSummaryDto>>> GetClientSummariesAsync();
        Task<ServiceResult<ClientProfileViewModel>> CreateClientProfileAsync(ClientProfileViewModel model);
        Task<ServiceResult<Guid>> EditClientProfileAsync(ClientProfileViewModel model);
        Task<ServiceResult<ClientProfileDetailDto>> GetClientProfileWithDetailsAsync(Guid clientId, bool freshmeat = false);
        Task<ServiceResult<Guid>> SaveClientProfileAsync(ClientProfileViewModel model, bool freshmeat);
        Task<ServiceResult<bool>> DeleteClientProfileAsync(Guid Id);
    }
}
