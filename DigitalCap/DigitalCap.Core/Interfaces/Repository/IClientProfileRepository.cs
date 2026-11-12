using DigitalCap.Core.Models;
using DigitalCap.Core.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IClientProfileRepository
    {
        Task<IReadOnlyList<Client>> GetAllClientsAsync(CancellationToken cancellationToken = default);
        Task AddAsync(Client client);
        
        Task<List<Client>> GetAllClients();
        Task<Client?> GetClientAsync(Guid Id);
        Task<Guid> SaveClientProfileAsync(Client client);
        Task DeleteClientAsync(Guid Id);
        Task<IEnumerable<ClientAdminDto>> GetClientAdminsAsync(Guid clientId);
    }

}
