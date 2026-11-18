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
    public class CosmosPortRepository : CosmosRepositoryBase<PortOfCallDocument>, IPortRepository
    {
        public CosmosPortRepository(ICosmosContainerFactory containerFactory, ILogger<CosmosPortRepository> logger)
            : base(containerFactory, CosmosContainers.core_PortOfCall, logger)
        {
        }

        /// <summary>
        /// Replaces stored procedure [CAP].[Port_Search].
        /// </summary>
        public async Task<IEnumerable<Port>> Search(string searchText)
        {
            QueryDefinition query;
            if (searchText == null)
            {
                query = new QueryDefinition("SELECT * FROM c");
            }
            else
            {
                query = new QueryDefinition("SELECT * FROM c WHERE CONTAINS(c.PortName, @searchText, true)")
                    .WithParameter("@searchText", searchText);
            }

            var documents = await QueryAsync(query);
            return documents.Select(d => new Port
            {
                Id = d.PortId,
                Code = d.PortCode!,
                Name = d.PortName!
                // CountryCode is not returned by CAP.Port_Search: leave null
            }).ToList();
        }

        /// <summary>
        /// Replaces inline query: SELECT PortId AS Id, PortCode AS Code, PortName AS Name, CountryCode FROM Core.PortOfCall WHERE PortId = @Id.
        /// </summary>
        public async Task<Port?> GetById(int id)
        {
            var idString = id.ToString();
            var doc = await ReadAsync(idString, new PartitionKey(idString));
            if (doc == null)
            {
                return null;
            }

            return new Port
            {
                Id = doc.PortId,
                Code = doc.PortCode!,
                Name = doc.PortName!,
                CountryCode = doc.CountryCode!
            };
        }
    }
}
