using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface ISecurityClientService
    {
        Task<ServiceResult<IReadOnlyList<ClientModel>>> GetAllClientsAsync();
    }
}

