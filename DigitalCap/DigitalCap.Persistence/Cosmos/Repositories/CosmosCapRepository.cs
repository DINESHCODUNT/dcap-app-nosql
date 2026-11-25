using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosCapRepository : CosmosRepositoryBase<ProjectDocument>, ICapRepository
    {
        private readonly Container _statusesContainer;
        private readonly Container _portOfCallContainer;
        private readonly Container _userLoginsContainer;
        private readonly Container _ptsContainer;

        public CosmosCapRepository(
            ICosmosContainerFactory containerFactory,
            ILogger<CosmosCapRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_Projects, logger)
        {
            _statusesContainer = containerFactory.GetContainer(CosmosContainers.CAP_Statuses);
            _portOfCallContainer = containerFactory.GetContainer(CosmosContainers.core_PortOfCall);
            _userLoginsContainer = containerFactory.GetContainer(CosmosContainers.dbo_AspNetUserLogins);
            _ptsContainer = containerFactory.GetContainer(CosmosContainers.CAP_ProjectTaskStatus);
        }

        public void Commit() { }

        public async Task<List<ProjectsGrid>> GetCapIndexAsync()
        {
            try
            {
                var result = await Sp_Read_Project();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<ProjectCharts>> GetProjectByStatusesAsync()
        {
            var result = await Sp_Read_Project_Statuses();
            return result;
        }

        public async Task<List<ProjectCharts>> GetCertsByRegionAsync()
        {
            try
            {
                var result = await Sp_Read_Certs_ByRegion();
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

        public async Task<List<ProjectChartsCount>> GetProjectCountAsync()
        {
            try
            {
                var scalar = await Sp_Read_Projects_Count();
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
                var result = await Sp_Read_Certs_Count();
                return result;
            }
            catch (Exception)
            {
                return new List<ProjectChartsCount>();
            }
        }

        #region Stored Procedure Helpers

        /// <summary>
        /// Replaces [CAP].[Read_Project] for ProjectsGrid.
        /// Inner joins CAP_Statuses, left joins core_PortOfCall and dbo_AspNetUserLogins (on CreatedBy and StatusChangedBy).
        /// Notice: no IsDeleted filter in SQL; 2 logins -> 2 rows.
        /// </summary>
        private async Task<List<ProjectsGrid>> Sp_Read_Project()
        {
            var projects = await QueryAsync(new QueryDefinition("SELECT * FROM c"));

            // Load Statuses
            var statuses = new Dictionary<int, string?>();
            using (var feed = _statusesContainer.GetItemQueryIterator<StatusDocument>(new QueryDefinition("SELECT c.ID, c.Status FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var s in resp)
                    {
                        statuses[s.ID] = s.Status;
                    }
                }
            }

            // Load Logins
            var loginsByUserId = new Dictionary<string, List<AspNetUserLoginDocument>>(StringComparer.OrdinalIgnoreCase);
            using (var feed = _userLoginsContainer.GetItemQueryIterator<AspNetUserLoginDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var l in resp)
                    {
                        if (!string.IsNullOrEmpty(l.UserId))
                        {
                            if (!loginsByUserId.TryGetValue(l.UserId, out var list))
                            {
                                list = new List<AspNetUserLoginDocument>();
                                loginsByUserId[l.UserId] = list;
                            }
                            list.Add(l);
                        }
                    }
                }
            }

            var result = new List<ProjectsGrid>();
            foreach (var p in projects)
            {
                // INNER JOIN [CAP].[Statuses] s ON s.ID = p.ProjectStatus
                if (!p.ProjectStatus.HasValue || !statuses.TryGetValue(p.ProjectStatus.Value, out var statusName))
                {
                    continue;
                }

                // LEFT JOIN AspNetUserLogins u on p.CreatedBy = u.UserId
                List<AspNetUserLoginDocument?> uList;
                if (!string.IsNullOrEmpty(p.CreatedBy) && loginsByUserId.TryGetValue(p.CreatedBy, out var uLogins) && uLogins.Count > 0)
                {
                    uList = uLogins.Cast<AspNetUserLoginDocument?>().ToList();
                }
                else
                {
                    uList = new List<AspNetUserLoginDocument?> { null };
                }

                // LEFT JOIN AspNetUserLogins u2 on p.StatusChangedBy = u2.UserId
                List<AspNetUserLoginDocument?> u2List;
                if (!string.IsNullOrEmpty(p.StatusChangedBy) && loginsByUserId.TryGetValue(p.StatusChangedBy, out var u2Logins) && u2Logins.Count > 0)
                {
                    u2List = u2Logins.Cast<AspNetUserLoginDocument?>().ToList();
                }
                else
                {
                    u2List = new List<AspNetUserLoginDocument?> { null };
                }

                foreach (var _ in uList)
                {
                    foreach (var _2 in u2List)
                    {
                        result.Add(new ProjectsGrid
                        {
                            ID = p.EffectiveId,
                            Name = p.Name!,
                            ProjectStatusName = statusName,
                            CapRegion = p.CapRegion!,
                            IMO = p.IMO,
                            VesselName = p.VesselName!,
                            PIDNumber = p.PIDNumberInt,
                            ProjectPriority = p.ProjectPriority,
                            CompanyName = p.CompanyName!,
                            WCN = p.WCN!
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Replaces [CAP].[Read_Project_Statuses].
        /// SELECT ID, [Name] = [Status] FROM [CAP].[Statuses] WHERE [Type] = 'Project'
        /// Note: Dapper maps columns ID and Name to ProjectCharts (Category, Value, Color) -> all defaults.
        /// </summary>
        private async Task<List<ProjectCharts>> Sp_Read_Project_Statuses()
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.Type), 'Project', true)");
            var result = new List<ProjectCharts>();
            using var feed = _statusesContainer.GetItemQueryIterator<StatusDocument>(query);
            while (feed.HasMoreResults)
            {
                var resp = await feed.ReadNextAsync();
                foreach (var s in resp)
                {
                    result.Add(new ProjectCharts
                    {
                        Category = null!,
                        Value = 0,
                        Color = null!
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// Replaces [CAP].[Read_Certs_ByRegion].
        /// SQL BUG KEPT: SP ends with FOR JSON PATH. SQL Server splits JSON output into 2033-char chunks.
        /// Dapper's QueryAsync&lt;ProjectCharts&gt; does not match the JSON column name, returning 1 default ProjectCharts per chunk.
        /// </summary>
        private async Task<List<ProjectCharts>> Sp_Read_Certs_ByRegion()
        {
            // 1. Read PTS where TaskId = 31 and StatusId = 1
            var ptsQuery = new QueryDefinition("SELECT c.ProjectId FROM c WHERE c.TaskId = 31 AND c.StatusId = 1");
            var projectIds = new List<string>();
            using (var feed = _ptsContainer.GetItemQueryIterator<ProjectTaskStatusDocument>(ptsQuery))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var pts in resp)
                    {
                        if (pts.ProjectId.HasValue)
                        {
                            projectIds.Add(pts.ProjectId.Value.ToString());
                        }
                    }
                }
            }

            if (projectIds.Count == 0)
            {
                return new List<ProjectCharts>();
            }

            // 2. Read Projects to get CapRegion
            var projectsQuery = new QueryDefinition("SELECT c.id, c.CapRegion FROM c WHERE ARRAY_CONTAINS(@ids, c.id)")
                .WithParameter("@ids", projectIds.Distinct().ToList());

            var projectsById = new Dictionary<string, ProjectDocument>();
            using (var feed = Container.GetItemQueryIterator<ProjectDocument>(projectsQuery))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var p in resp)
                    {
                        projectsById[p.id] = p;
                    }
                }
            }

            // Inner join PTS and Projects
            var grouped = projectIds
                .Where(id => projectsById.ContainsKey(id))
                .Select(id => projectsById[id].CapRegion)
                .GroupBy(r => r)
                .Select(g => new
                {
                    Category = g.Key,
                    Value = g.Count(),
                    Color = (g.Key == "Americas/Europe" || g.Key == "Americas / Europe") ? "rgb(0,47,108)"
                          : g.Key == "Singapore" ? "rgb(255,82,05)"
                          : g.Key == "Greece" ? "rgb(99,102,106)"
                          : "rgb(0,133,173)"
                })
                .ToList();

            if (grouped.Count == 0)
            {
                return new List<ProjectCharts>();
            }

            // Build JSON text as SQL Server FOR JSON PATH would produce
            var jsonString = JsonConvert.SerializeObject(grouped);
            int chunks = (int)Math.Ceiling((double)jsonString.Length / 2033.0);
            return Enumerable.Range(0, Math.Max(1, chunks)).Select(_ => new ProjectCharts()).ToList();
        }

        /// <summary>
        /// Replaces [CAP].[Read_Projects_Count].
        /// SELECT count([ID]) AS [Count] FROM [CAP].[Projects] FOR JSON PATH
        /// </summary>
        private async Task<string> Sp_Read_Projects_Count()
        {
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            int total = 0;
            using var feed = Container.GetItemQueryIterator<int>(countQuery);
            while (feed.HasMoreResults)
            {
                var resp = await feed.ReadNextAsync();
                total += resp.Sum();
            }

            return $"[{{\"Count\":{total}}}]";
        }

        /// <summary>
        /// Replaces [CAP].[Read_Certs_Count].
        /// SELECT count(p.[ID]) as [Count] FROM [CAP].[ProjectTaskStatus] pts INNER JOIN [CAP].[Projects] p on pts.[ProjectId] = p.[ID] WHERE pts.[TaskId] = 31 and pts.[StatusId] = 1 FOR JSON PATH
        /// Note: in Dapper, QueryAsync&lt;ProjectChartsCount&gt; against FOR JSON PATH receives 1 row with null Count due to JSON column name.
        /// </summary>
        private async Task<List<ProjectChartsCount>> Sp_Read_Certs_Count()
        {
            var ptsQuery = new QueryDefinition("SELECT c.ProjectId FROM c WHERE c.TaskId = 31 AND c.StatusId = 1");
            var projectIds = new List<string>();
            using (var feed = _ptsContainer.GetItemQueryIterator<ProjectTaskStatusDocument>(ptsQuery))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var pts in resp)
                    {
                        if (pts.ProjectId.HasValue)
                        {
                            projectIds.Add(pts.ProjectId.Value.ToString());
                        }
                    }
                }
            }

            if (projectIds.Count == 0)
            {
                return new List<ProjectChartsCount> { new ProjectChartsCount() };
            }

            // Join with Projects
            var distinctIds = projectIds.Distinct().ToList();
            var projectsQuery = new QueryDefinition("SELECT c.id FROM c WHERE ARRAY_CONTAINS(@ids, c.id)")
                .WithParameter("@ids", distinctIds);

            var existingProjectIds = new HashSet<string>();
            using (var feed = Container.GetItemQueryIterator<ProjectDocument>(projectsQuery))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var p in resp)
                    {
                        existingProjectIds.Add(p.id);
                    }
                }
            }

            return new List<ProjectChartsCount> { new ProjectChartsCount() };
        }

        #endregion
    }
}
