using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosClientRepository : CosmosRepositoryBase<ClientsDocument>, IClientRepository
    {
        public CosmosClientRepository(ICosmosContainerFactory containerFactory, ILogger<CosmosClientRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_Clients, logger)
        {
        }

        /// <summary>
        /// Replaces DapperExtensions GetListAsync&lt;Client&gt; on [CAP].[Clients].
        /// </summary>
        public async Task<List<Client>> GetAllClients()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var documents = await QueryAsync(query);
            return documents.Select(d => new Client
            {
                Id = d.Id,
                Wcn = d.Wcn,
                CompanyName = d.CompanyName!,
                CompanyAddress = d.CompanyAddress!,
                CompanyBillingEmail = d.CompanyBillingEmail!,
                CompanyBillingSystemUrl = d.CompanyBillingSystemUrl!
                // Ignored properties keep their default values
            }).ToList();
        }
    }
}
