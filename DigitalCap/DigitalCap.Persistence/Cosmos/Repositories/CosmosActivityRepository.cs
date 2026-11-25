using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosActivityRepository : CosmosRepositoryBase<ActivityDocument>, IActivityRepository
    {
        private readonly Container _projectsContainer;

        public CosmosActivityRepository(
            ICosmosContainerFactory containerFactory,
            ILogger<CosmosActivityRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_Activities, logger)
        {
            _projectsContainer = containerFactory.GetContainer(CosmosContainers.CAP_Projects);
        }

        public async Task<List<RecentActivity>> GetRecentActivitiesAsync(int take)
        {
            try
            {
                var query = new QueryDefinition("SELECT TOP @take * FROM c ORDER BY c.CreateDttm DESC")
                    .WithParameter("@take", take);

                var activities = await QueryAsync(query);
                if (activities.Count == 0)
                {
                    return new List<RecentActivity>();
                }

                var projectIds = activities
                    .Where(a => a.ProjectId.HasValue)
                    .Select(a => a.ProjectId!.Value.ToString())
                    .Distinct()
                    .ToList();

                var projectsById = new Dictionary<int, ProjectDocument>();
                if (projectIds.Count > 0)
                {
                    var projQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@ids, c.id)")
                        .WithParameter("@ids", projectIds);

                    using var feed = _projectsContainer.GetItemQueryIterator<ProjectDocument>(projQuery);
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var p in response)
                        {
                            projectsById[p.EffectiveId] = p;
                        }
                    }
                }

                var result = new List<RecentActivity>(activities.Count);
                foreach (var a in activities)
                {
                    ProjectDocument? p = null;
                    if (a.ProjectId.HasValue)
                    {
                        projectsById.TryGetValue(a.ProjectId.Value, out p);
                    }

                    result.Add(new RecentActivity
                    {
                        ID = a.ID,
                        Text = a.Text!,
                        CreateBy = a.CreateBy!,
                        CreateDttm = a.CreateDttm ?? default(DateTime),
                        ProjectId = a.ProjectId ?? 0,
                        VesselName = p?.VesselName!,
                        PIDNumber = p?.PIDNumberInt ?? 0
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching recent activities");
                throw;
            }
        }

        public async Task<List<RecentActivity>> GetRecentActivitiesByProjectIdAsync(int size, int projectId)
        {
            try
            {
                // INNER JOIN: if project does not exist, returns empty list
                ProjectDocument? project = null;
                try
                {
                    var response = await _projectsContainer.ReadItemAsync<ProjectDocument>(
                        projectId.ToString(),
                        new PartitionKey(projectId.ToString()));
                    project = response.Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new List<RecentActivity>();
                }

                if (project == null)
                {
                    return new List<RecentActivity>();
                }

                var query = new QueryDefinition("SELECT TOP @size * FROM c WHERE c.ProjectId = @projectId ORDER BY c.CreateDttm DESC")
                    .WithParameter("@size", size)
                    .WithParameter("@projectId", projectId);

                var pk = new PartitionKey((double)projectId);
                var activities = await QueryAsync(query, pk);

                var result = activities.Select(a => new RecentActivity
                {
                    ID = a.ID,
                    Text = a.Text!,
                    CreateBy = a.CreateBy!,
                    CreateDttm = a.CreateDttm ?? default(DateTime),
                    ProjectId = projectId,
                    VesselName = project.VesselName!,
                    PIDNumber = project.PIDNumberInt ?? 0
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching activities by projectId");
                throw;
            }
        }

        public async Task<List<RecentActivity>> GetRecentActivitiesAsync2(int page, int size)
        {
            try
            {
                var allActivities = await QueryAsync(new QueryDefinition("SELECT * FROM c"));

                var projectIds = allActivities
                    .Where(a => a.ProjectId.HasValue)
                    .Select(a => a.ProjectId!.Value.ToString())
                    .Distinct()
                    .ToList();

                var projectsById = new Dictionary<int, ProjectDocument>();
                if (projectIds.Count > 0)
                {
                    var projQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@ids, c.id)")
                        .WithParameter("@ids", projectIds);

                    using var feed = _projectsContainer.GetItemQueryIterator<ProjectDocument>(projQuery);
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var p in response)
                        {
                            projectsById[p.EffectiveId] = p;
                        }
                    }
                }

                var fullList = new List<RecentActivity>();
                foreach (var a in allActivities)
                {
                    if (a.ProjectId.HasValue)
                    {
                        if (projectsById.TryGetValue(a.ProjectId.Value, out var p))
                        {
                            fullList.Add(new RecentActivity
                            {
                                ID = a.ID,
                                Text = a.Text!,
                                CreateBy = a.CreateBy!,
                                CreateDttm = a.CreateDttm ?? default(DateTime),
                                PIDNumber = p.PIDNumberInt ?? 0,
                                ProjectId = p.EffectiveId,
                                VesselName = p.VesselName!
                            });
                        }
                        // If project not found, row is dropped (INNER JOIN in view)
                    }
                    else
                    {
                        fullList.Add(new RecentActivity
                        {
                            ID = a.ID,
                            Text = a.Text!,
                            CreateBy = a.CreateBy!,
                            CreateDttm = a.CreateDttm ?? default(DateTime),
                            PIDNumber = 0,
                            ProjectId = 0,
                            VesselName = string.Empty
                        });
                    }
                }

                // SQL UNION eliminates exact duplicate rows
                var distinctList = fullList
                    .GroupBy(x => (x.ID, x.Text, x.CreateBy, x.CreateDttm, x.PIDNumber, x.ProjectId, x.VesselName))
                    .Select(g => g.First())
                    .OrderBy(x => x.CreateDttm == default ? 1 : 0)
                    .ThenByDescending(x => x.CreateDttm)
                    .Skip(page * size)
                    .Take(size)
                    .ToList();

                return distinctList;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching paged recent activities");
                throw;
            }
        }
    }
}
