using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using System.Data;
using Dapper;
using Newtonsoft.Json;

namespace DigitalCap.Persistence.Repositories
{
    public class CapRepository : ICapRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;
        public CapRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        public void Commit()
        {
            _unitOfWork?.Commit();
        }
        
        public async Task<List<ProjectsGrid>> GetCapIndexAsync()
        {
            try
            {
                var result = await Connection.QueryAsync<ProjectsGrid>(
                sql: "[CAP].[Read_Project]",
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

                return result.AsList();
            }
            catch (Exception sqlEx)
            {
                throw;
            }
        }

        public async Task<List<ProjectCharts>> GetProjectByStatusesAsync()
        {
            var result = await Connection.QueryAsync<ProjectCharts>(
            sql : "[CAP].[Read_Project_Statuses]",
            commandType: CommandType.StoredProcedure,
            transaction: Transaction
        );

            return result.ToList();
        }

        

        public async Task<List<ProjectCharts>> GetCertsByRegionAsync()
        {
            try
            {
                var result = await Connection.QueryAsync<ProjectCharts>(
                sql: "[CAP].[Read_Certs_ByRegion]",
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);

                var list = result.ToList();

                // Preserve original empty logic
                if (list == null || !list.Any())
                {
                    return new List<ProjectCharts>
                    {
                        new ProjectCharts
                        {
                        Category = "",
                        Value = 1,
                        Color = "rgb(219,220,198)"
                        }
                    };
                }

                return list;
            }
            catch (Exception)
            {
                return new List<ProjectCharts>();
            }
        }


        /// <summary>
        /// Matches legacy MVC: SP returns a single cell containing JSON for <see cref="ProjectChartsCount"/>.
        /// If your proc instead returns a rowset with a Count column, use QueryAsync&lt;ProjectChartsCount&gt; instead.
        /// </summary>
        public async Task<List<ProjectChartsCount>> GetProjectCountAsync()
        {
            try
            {
                var scalar = await Connection.ExecuteScalarAsync(
                    "[CAP].[Read_Projects_Count]",
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure);

                var jsonString = scalar?.ToString();
                if (string.IsNullOrWhiteSpace(jsonString))
                    return new List<ProjectChartsCount>();

                var list = JsonConvert.DeserializeObject<List<ProjectChartsCount>>(jsonString);
                return list ?? new List<ProjectChartsCount>();
            }
            catch (Exception)
            {
                return new List<ProjectChartsCount>();
            }
        }


        public async Task<List<ProjectChartsCount>> GetCertsCountAsync()
        {
            try
            {
                var result = await Connection.QueryAsync<ProjectChartsCount>(
                sql: "[CAP].[Read_Certs_Count]",
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);

                return result.AsList();
            }
            catch (Exception)
            {
                return new List<ProjectChartsCount>();
            }
        }

    }

}
