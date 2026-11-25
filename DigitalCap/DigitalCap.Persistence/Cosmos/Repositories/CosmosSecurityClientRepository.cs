using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosSecurityClientRepository : CosmosRepositoryBase<ClientsDocument>, ISecurityClientRepository
    {
        public CosmosSecurityClientRepository(
            ICosmosContainerFactory containerFactory,
            ILogger<CosmosSecurityClientRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_Clients, logger)
        {
        }

        public async Task<IReadOnlyList<ClientModel>> GetAllClientsAsync()
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c");
                var documents = await QueryAsync(query);
                return documents.Select(d => new ClientModel
                {
                    Id = d.Id,
                    WCN = d.Wcn!,
                    CompanyName = d.CompanyName!
                }).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching all clients");
                throw;
            }
        }

        #region Unused IRepositoryBase members (Rule 58)
        public Task<Guid> InsertAsync(ClientModel entity) => throw new NotSupportedException("SecurityClientRepository.InsertAsync is not used by the app and is not converted to Cosmos.");
        public Guid Insert(ClientModel entity) => throw new NotSupportedException("SecurityClientRepository.Insert is not used by the app and is not converted to Cosmos.");
        public Task<bool> UpdateAsync(ClientModel entity) => throw new NotSupportedException("SecurityClientRepository.UpdateAsync is not used by the app and is not converted to Cosmos.");
        public bool Update(ClientModel entity) => throw new NotSupportedException("SecurityClientRepository.Update is not used by the app and is not converted to Cosmos.");
        public Task<bool> DeleteAsync(Guid id) => throw new NotSupportedException("SecurityClientRepository.DeleteAsync is not used by the app and is not converted to Cosmos.");
        public bool Delete(Guid id) => throw new NotSupportedException("SecurityClientRepository.Delete is not used by the app and is not converted to Cosmos.");
        public Task<ClientModel?> GetAsync(Guid id, bool includeDeleted = false, bool useTransaction = true) => throw new NotSupportedException("SecurityClientRepository.GetAsync is not used by the app and is not converted to Cosmos.");
        public ClientModel Get(Guid id, bool includeDeleted = false, bool useTransaction = true) => throw new NotSupportedException("SecurityClientRepository.Get is not used by the app and is not converted to Cosmos.");
        public Task<IQueryable<ClientModel>> GetAllAsync(bool useTransaction = true) => throw new NotSupportedException("SecurityClientRepository.GetAllAsync is not used by the app and is not converted to Cosmos.");
        public IQueryable<ClientModel> GetFiltered(Expression<Func<ClientModel, bool>> filter) => throw new NotSupportedException("SecurityClientRepository.GetFiltered is not used by the app and is not converted to Cosmos.");
        public ClientModel FirstOrDefault(Expression<Func<ClientModel, bool>> filter) => throw new NotSupportedException("SecurityClientRepository.FirstOrDefault is not used by the app and is not converted to Cosmos.");
        public ClientModel First(Expression<Func<ClientModel, bool>> filter) => throw new NotSupportedException("SecurityClientRepository.First is not used by the app and is not converted to Cosmos.");
        public IQueryable<ClientModel> ExecuteQuery(string sql) => throw new NotSupportedException("SecurityClientRepository.ExecuteQuery is not used by the app and is not converted to Cosmos.");
        public IQueryable<ClientModel> ExecuteQuery(string sql, object[] parameters) => throw new NotSupportedException("SecurityClientRepository.ExecuteQuery is not used by the app and is not converted to Cosmos.");
        public void Rollback() => throw new NotSupportedException("SecurityClientRepository.Rollback is not used by the app and is not converted to Cosmos.");
        public void Commit() => throw new NotSupportedException("SecurityClientRepository.Commit is not used by the app and is not converted to Cosmos.");
        #endregion
    }
}
