using Dapper;
using DapperExtensions;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DigitalCap.Persistence.Repositories
{
    public class ActivityRepository: IActivityRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ActivityRepository> _logger;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;
        public ActivityRepository(IUnitOfWork unitOfWork, ILogger<ActivityRepository> logger) 
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<List<RecentActivity>> GetRecentActivitiesAsync(int take)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Take", take);

                var result = await Connection.QueryAsync<RecentActivity>(
                sql: "[CAP].[Read_Activities]",
                param: parameters,
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent activities");
                throw;
            }
        }


        public async Task<List<RecentActivity>> GetRecentActivitiesAsync2(int page, int size)
        {
            try
            {
                var sort = new List<ISort>
                {
                    Predicates.Sort<RecentActivity>(x => x.CreateDttm, false)
                };

                // Retrieve all activities without any date filtering
                var results = await Connection.GetPageAsync<RecentActivity>(
                    null, sort, page, size, transaction: Transaction);
                return results.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paged recent activities");
                throw;
            }
        }

        public async Task<List<RecentActivity>> GetRecentActivitiesByProjectIdAsync(int size, int projectId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Take", size);
                parameters.Add("@ProjectId", projectId);

                var result = await Connection.QueryAsync<RecentActivity>(
            sql: "[CAP].[Read_Activities_ByProjectId]",
            param: parameters,
            commandType: CommandType.StoredProcedure,
            transaction: Transaction
        );

                return result.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching activities by projectId");
                throw;
            }
        }
    }
}
