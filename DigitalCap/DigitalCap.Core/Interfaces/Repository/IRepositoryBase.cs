using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IRepositoryBase<TEntity, TKey>
        where TEntity : IEntity<TKey>
    {
        Task<TKey> InsertAsync(TEntity entity);

        TKey Insert(TEntity entity);

        Task<bool> UpdateAsync(TEntity entity);

        bool Update(TEntity entity);

        Task<bool> DeleteAsync(TKey id);

        bool Delete(TKey id);

        Task<TEntity?> GetAsync(TKey id, bool includeDeleted = false, bool useTransaction = true);

        TEntity Get(TKey id, bool includeDeleted = false, bool useTransaction = true);

        Task<IQueryable<TEntity>> GetAllAsync(bool useTransaction = true);

        IQueryable<TEntity> GetFiltered(Expression<Func<TEntity, bool>> filter);

        TEntity FirstOrDefault(Expression<Func<TEntity, bool>> filter);

        TEntity First(Expression<Func<TEntity, bool>> filter);

        IQueryable<TEntity> ExecuteQuery(string sql);

        IQueryable<TEntity> ExecuteQuery(string sql, object[] parameters);

        /// <summary>
        /// Discards any changes made during the current transaction. If any other repositories share the transaction, their changes will also be discarded.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Commits any changes made during the current transaction. If any other repositories share the transaction, their changes will also be committed.
        /// </summary>
        void Commit();
    }
}
