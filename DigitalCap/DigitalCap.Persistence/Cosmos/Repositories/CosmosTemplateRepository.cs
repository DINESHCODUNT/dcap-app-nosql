using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosTemplateRepository : CosmosRepositoryBase<object>, ITemplateRepository
    {
        public CosmosTemplateRepository(
            ICosmosContainerFactory containerFactory,
            ILogger<CosmosTemplateRepository> logger)
            : base(containerFactory, CosmosContainers.upskill_ReportTemplates, logger)
        {
        }

        public void Commit() { }

        public async Task<byte[]> PopulateTemplate(int projectId)
        {
            try
            {
                var result = await Sp_PopulateTemplate(projectId);
                return result ?? new byte[0];
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }

        #region Stored Procedure Helpers

        private Task<byte[]?> Sp_PopulateTemplate(int projectId)
        {
            // [upskill].[sp_PopulateTemplate] does not exist in SQL schema.
            // Throw NotSupportedException so C# caller catches and returns new byte[0].
            throw new NotSupportedException("[upskill].[sp_PopulateTemplate] does not exist.");
        }

        #endregion
    }
}
