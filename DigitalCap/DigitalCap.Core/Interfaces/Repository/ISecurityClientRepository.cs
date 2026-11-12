using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface ISecurityClientRepository
        : IRepositoryBase<ClientModel, Guid>
    {
        Task<IReadOnlyList<ClientModel>> GetAllClientsAsync();
    }
}
