using Dapper;
using DapperExtensions;
using DigitalCap.Core.Helpers;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq.Expressions;

namespace DigitalCap.Persistence.Repositories
{
    public abstract class RepositoryBase<TEntity, TKey>
        : IRepositoryBase<TEntity, TKey>
        where TEntity : EntityBase<TKey>
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly ILogger _logger;
        protected readonly IDbProvider? _dbProvider;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;
        protected RepositoryBase(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        protected RepositoryBase(IUnitOfWork unitOfWork, IDbProvider dbProvider)
        {
            _unitOfWork = unitOfWork;
            _dbProvider = dbProvider;
        }
        protected RepositoryBase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        protected RepositoryBase(IDbProvider dbProvider)
        {
            _dbProvider = dbProvider;
        }

        public void Rollback()
        {
            _unitOfWork?.Rollback();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Commit()
        {
            _unitOfWork?.Commit();
        }

        public virtual async Task<TEntity?> GetAsync(TKey id, bool includeDeleted = false, bool useTransaction = true)
        {
            try
            {
                var result = await Connection.GetAsync<TEntity?>(id, useTransaction ? Transaction : null);
                if (result == null || !includeDeleted && result.Deleted) return null;
                return result;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="useTransaction"></param>
        /// <returns></returns>
        public virtual TEntity Get(TKey id, bool includeDeleted = false, bool useTransaction = true)
        {
            var result = Connection.Get<TEntity?>(id, useTransaction ? Transaction : null);
            if (result == null || !includeDeleted && result.Deleted) return null!;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="useTransaction"></param>
        /// <returns></returns>
        public virtual async Task<IQueryable<TEntity>> GetAllAsync(bool useTransaction = true)
        {
            var whereClause = "(Deleted = 0)";
            var tableName = typeof(TEntity).CustomAttributes
                .FirstOrDefault(x => x.AttributeType.Name.Equals("TableAttribute"))!
                .ConstructorArguments[0].Value;
            var schemaName = typeof(TEntity).CustomAttributes
               .First(x => x.AttributeType.Name.Equals("TableAttribute"))
               .NamedArguments;
            if (schemaName.Count > 0)
            {
                tableName = schemaName[0].TypedValue.Value + "." + tableName;
            }
            return await Task.Run(() => ExecuteQuery($"SELECT * FROM {tableName} WHERE {whereClause}"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> GetFiltered(Expression<Func<TEntity, bool>> filter)
        {
            var whereClause = LinqHelper.ToWhereClause(filter) + " AND (Deleted = 0)";
            var tableName = typeof(TEntity).CustomAttributes
                .FirstOrDefault(x => x.AttributeType.Name.Equals("TableAttribute"))!
                .ConstructorArguments[0].Value;
            var schemaName = typeof(TEntity).CustomAttributes
               .First(x => x.AttributeType.Name.Equals("TableAttribute"))
               .NamedArguments;
            if (schemaName.Count > 0)
            {
                tableName = $"[{schemaName[0].TypedValue.Value}] . [{tableName}]";
            }
            return ExecuteQuery($"SELECT * FROM {tableName} WHERE {whereClause}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual TEntity FirstOrDefault(Expression<Func<TEntity, bool>> filter) => GetFiltered(filter).FirstOrDefault()!;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual TEntity First(Expression<Func<TEntity, bool>> filter) => GetFiltered(filter).First();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> ExecuteQuery(string sql)
        {
            var result = Connection.Query<TEntity>(
                sql: sql,
                commandType: CommandType.Text,
                transaction: Transaction);
            return result.AsQueryable();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> ExecuteQuery(string sql, object[] parameters)
        {
            var result = Connection.Query<TEntity>(
                sql: sql,
                commandType: CommandType.Text,
                param: parameters,
                transaction: Transaction);
            return result.AsQueryable();
        }

        public virtual async Task<TKey> InsertAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Generate new ID if Guid and empty
            if (typeof(TKey) == typeof(Guid) && entity.Id is Guid id && id == Guid.Empty)
            {
                var newGuid = Guid.NewGuid();
                entity.Id = (TKey)(object)newGuid;
            }

            // Set CreatedAt for AuditableEntity
            if (entity is IAuditableEntity<TKey> auditableEntity)
            {
                if (auditableEntity.CreatedAt == null || auditableEntity.CreatedAt == DateTime.MinValue)
                {
                    auditableEntity.CreatedAt = DateTime.UtcNow;
                }
            }

            // Use DapperExtensions to insert
            await Task.Run(() => Connection.Insert(entity, Transaction));
            
            return entity.Id;
        }

        public virtual TKey Insert(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Generate new ID if Guid and empty
            if (typeof(TKey) == typeof(Guid) && entity.Id is Guid id && id == Guid.Empty)
            {
                var newGuid = Guid.NewGuid();
                entity.Id = (TKey)(object)newGuid;
            }

            // Set CreatedAt for AuditableEntity
            if (entity is IAuditableEntity<TKey> auditableEntity)
            {
                if (auditableEntity.CreatedAt == null || auditableEntity.CreatedAt == DateTime.MinValue)
                {
                    auditableEntity.CreatedAt = DateTime.UtcNow;
                }
            }

            // Use DapperExtensions to insert
            Connection.Insert(entity, Transaction);
            
            return entity.Id;
        }

        public virtual async Task<bool> UpdateAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.Id == null || (entity.Id is Guid guid && guid == Guid.Empty))
                throw new ArgumentException("Entity Id cannot be null or empty for update operation", nameof(entity));

            // Set UpdatedAt for AuditableEntity
            if (entity is IAuditableEntity<TKey> auditableEntity)
            {
                auditableEntity.UpdatedAt = DateTime.UtcNow;
            }

            // Use DapperExtensions to update
            var result = await Task.Run(() => Connection.Update(entity, Transaction));
            return result;
        }

        public virtual bool Update(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.Id == null || (entity.Id is Guid guid && guid == Guid.Empty))
                throw new ArgumentException("Entity Id cannot be null or empty for update operation", nameof(entity));

            // Set UpdatedAt for AuditableEntity
            if (entity is IAuditableEntity<TKey> auditableEntity)
            {
                auditableEntity.UpdatedAt = DateTime.UtcNow;
            }

            // Use DapperExtensions to update
            return Connection.Update(entity, Transaction);
        }

        public virtual async Task<bool> DeleteAsync(TKey id)
        {
            var entity = await GetAsync(id);

            if (entity != null)
            {
                entity.Deleted = true;
                var result = await UpdateAsync(entity);
                return result;
            }
            else
            {
                // Handle the case where the entity was not found, e.g., by returning false or throwing an exception.
                return false;
            }
        }
        public bool Delete(TKey id)
        {
            throw new NotImplementedException();
        }
    }
}
