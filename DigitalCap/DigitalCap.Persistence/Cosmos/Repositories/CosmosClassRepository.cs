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
    public class CosmosClassRepository : CosmosRepositoryBase<ClassesDocument>, IClassRepository
    {
        public CosmosClassRepository(ICosmosContainerFactory containerFactory, ILogger<CosmosClassRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_Classes, logger)
        {
        }

        /// <summary>
        /// Replaces stored procedure [CAP].[Class_Search].
        /// </summary>
        public async Task<IEnumerable<ClassModel>> Search()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var documents = await QueryAsync(query);
            return documents.Select(d => new ClassModel
            {
                Id = d.ID,
                Name = d.Name!,
                Abbreviation = d.Abbreviation!
            }).ToList();
        }
    }
}
