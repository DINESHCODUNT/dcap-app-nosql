using DigitalCap.Core.Interfaces.Repository;
using System;
using System.Data;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    /// <summary>
    /// Cosmos-only build: there is no SQL connection or transaction. Services still call Begin/Commit/Rollback
    /// (e.g. CommentService.Commit), which are no-ops here – Cosmos writes are already saved per operation.
    /// </summary>
    public class CosmosUnitOfWork : IUnitOfWork
    {
        public IDbConnection Connection => throw new NotSupportedException("No SQL connection in the Cosmos-only build.");
        public IDbTransaction Transaction => null!;
        public string ConnectionString => string.Empty;
        public void Begin() { }
        public void Commit() { }
        public void Rollback() { }
    }
}
