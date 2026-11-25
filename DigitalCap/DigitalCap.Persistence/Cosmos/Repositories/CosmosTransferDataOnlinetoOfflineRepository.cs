using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    /// <summary>Cosmos version of TransferDataOnlinetoOfflineRepository (the 2 methods the web screens use).</summary>
    public class CosmosTransferDataOnlinetoOfflineRepository : CosmosRepositoryBase<IsSynchedOnlineDocument>, ITransferDataOnlinetoOfflineRepository
    {
        private readonly CosmosActivityWriter _activityWriter;
        private readonly ILogger<CosmosTransferDataOnlinetoOfflineRepository> _logger;

        public CosmosTransferDataOnlinetoOfflineRepository(
            ICosmosContainerFactory containerFactory,
            CosmosActivityWriter activityWriter,
            ILogger<CosmosTransferDataOnlinetoOfflineRepository> logger)
            : base(containerFactory, CosmosContainers.dbo_IsSynchedOnline, logger)
        {
            _activityWriter = activityWriter;
            _logger = logger;
        }

        /// <summary>Replaces dbo.GetDownloadOfflineProject. QueryFirstOrDefault without ORDER BY: lowest Id.
        /// Dapper: a NULL column keeps the model default.</summary>
        public async Task<IsSynchedOnline> GetDownloadOfflineProjects(int projectId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @p AND c.IsSynched = false AND c.IsReadOnly = false")
                .WithParameter("@p", projectId);
            var docs = await QueryAsync(query, new PartitionKey((double)projectId));
            var d = docs.OrderBy(x => x.Id).FirstOrDefault();
            if (d == null)
            {
                return null!;
            }

            var model = new IsSynchedOnline
            {
                Id = d.Id ?? 0,
                ProjectId = d.ProjectId ?? 0,
                UserId = d.UserId!,
                EmailId = d.EmailId!
            };
            if (d.DownloadOfflineDate.HasValue) model.DownloadOfflineDate = d.DownloadOfflineDate.Value;
            if (d.SynchedDate.HasValue) model.SynchedDate = d.SynchedDate.Value;
            if (d.IsSynched.HasValue) model.IsSynched = d.IsSynched.Value;
            return model;
        }

        /// <summary>Replaces dbo.ReadOnlyData. Same as the SQL C#: any error -> log and return false.</summary>
        public async Task<bool> ReleaseReadOnlyOnline(int projectId, string userName)
        {
            try
            {
                // @UserName varchar(40)
                var cutUser = userName != null && userName.Length > 40 ? userName.Substring(0, 40) : userName;
                await _activityWriter.CreateActivityAsync(projectId, "Admin Release the Project from Read Only Mode", cutUser);

                var pk = new PartitionKey((double)projectId);
                var query = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @p AND c.IsSynched = false AND c.IsReadOnly = false")
                    .WithParameter("@p", projectId);
                foreach (var doc in await QueryAsync(query, pk))
                {
                    doc.IsReadOnly = true;
                    doc.IsSynched = true;
                    await ReplaceAsync(doc, doc.id, pk);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ReleaseReadOnlyOnline");
                return false;
            }
        }

        #region Unused IRepositoryBase members (not used by the app; same pattern as CosmosSecurityClientRepository)
        public Task<Guid> InsertAsync(UserAccountModel entity) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.InsertAsync is not used by the app and is not converted to Cosmos.");
        public Guid Insert(UserAccountModel entity) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.Insert is not used by the app and is not converted to Cosmos.");
        public Task<bool> UpdateAsync(UserAccountModel entity) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.UpdateAsync is not used by the app and is not converted to Cosmos.");
        public bool Update(UserAccountModel entity) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.Update is not used by the app and is not converted to Cosmos.");
        public Task<bool> DeleteAsync(Guid id) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.DeleteAsync is not used by the app and is not converted to Cosmos.");
        public bool Delete(Guid id) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.Delete is not used by the app and is not converted to Cosmos.");
        public Task<UserAccountModel?> GetAsync(Guid id, bool includeDeleted = false, bool useTransaction = true) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.GetAsync is not used by the app and is not converted to Cosmos.");
        public UserAccountModel Get(Guid id, bool includeDeleted = false, bool useTransaction = true) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.Get is not used by the app and is not converted to Cosmos.");
        public Task<IQueryable<UserAccountModel>> GetAllAsync(bool useTransaction = true) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.GetAllAsync is not used by the app and is not converted to Cosmos.");
        public IQueryable<UserAccountModel> GetFiltered(Expression<Func<UserAccountModel, bool>> filter) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.GetFiltered is not used by the app and is not converted to Cosmos.");
        public UserAccountModel FirstOrDefault(Expression<Func<UserAccountModel, bool>> filter) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.FirstOrDefault is not used by the app and is not converted to Cosmos.");
        public UserAccountModel First(Expression<Func<UserAccountModel, bool>> filter) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.First is not used by the app and is not converted to Cosmos.");
        public IQueryable<UserAccountModel> ExecuteQuery(string sql) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.ExecuteQuery is not used by the app and is not converted to Cosmos.");
        public IQueryable<UserAccountModel> ExecuteQuery(string sql, object[] parameters) => throw new NotSupportedException("TransferDataOnlinetoOfflineRepository.ExecuteQuery is not used by the app and is not converted to Cosmos.");
        // Cosmos saves each write immediately: Commit/Rollback have nothing to do.
        public void Rollback() { }
        public void Commit() { }
        #endregion
    }
}
