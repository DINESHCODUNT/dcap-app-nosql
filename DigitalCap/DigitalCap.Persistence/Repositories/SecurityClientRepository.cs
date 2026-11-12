using Dapper;
using DigitalCap.Core.Models;
using DigitalCap.Core.Interfaces.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Repositories
{
    public class SecurityClientRepository
        : RepositoryBase<ClientModel, Guid>, ISecurityClientRepository
    {
        private new readonly ILogger<SecurityClientRepository> _logger;

        public SecurityClientRepository(
            IUnitOfWork unitOfWork,
            ILogger<SecurityClientRepository> logger)
            : base(unitOfWork, logger)
        {
            _logger = logger;
        }


        public async Task<IReadOnlyList<ClientModel>> GetAllClientsAsync()
        {
            try
            {
                var result = await Connection.QueryAsync<ClientModel>(
                    sql: "SELECT * FROM CAP.Clients",
                    transaction: Transaction);

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all clients");
                throw;
            }
        }
    }
}
