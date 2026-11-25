using DigitalCap.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public class ProjectStatusRecalculator
    {
        private readonly Container _ptsContainer;
        private readonly Container _projectsContainer;
        private readonly ILogger<ProjectStatusRecalculator> _logger;

        public ProjectStatusRecalculator(
            ICosmosContainerFactory containerFactory,
            ILogger<ProjectStatusRecalculator> logger)
        {
            if (containerFactory == null) throw new ArgumentNullException(nameof(containerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ptsContainer = containerFactory.GetContainer(CosmosContainers.CAP_ProjectTaskStatus);
            _projectsContainer = containerFactory.GetContainer(CosmosContainers.CAP_Projects);
        }

        public async Task<int> RecalculateAllAsync(CancellationToken ct = default)
        {
            // 1. Read all PTS rows: ProjectId, StatusId
            var ptsQuery = new QueryDefinition("SELECT c.ProjectId, c.StatusId FROM c");
            var allPts = new List<ProjectTaskStatusDocument>();
            using (var feed = _ptsContainer.GetItemQueryIterator<ProjectTaskStatusDocument>(ptsQuery))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync(ct);
                    allPts.AddRange(response);
                }
            }

            var ptsByProject = allPts
                .GroupBy(x => x.ProjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 2. Read all projects: id, PIDNumber, ProjectStatus
            var projQuery = new QueryDefinition("SELECT c.id, c.PIDNumber, c.ProjectStatus FROM c");
            var allProjects = new List<ProjectDocument>();
            using (var feed = _projectsContainer.GetItemQueryIterator<ProjectDocument>(projQuery))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync(ct);
                    allProjects.AddRange(response);
                }
            }

            int changedCount = 0;
            foreach (var p in allProjects)
            {
                ct.ThrowIfCancellationRequested();
                var projId = p.EffectiveId;

                bool hasPts = ptsByProject.TryGetValue(projId, out var projectPts);
                bool notExists3or2 = !hasPts || !projectPts.Any(pts => pts.StatusId == 3 || pts.StatusId == 2);
                bool exists2or1 = hasPts && projectPts.Any(pts => pts.StatusId == 2 || pts.StatusId == 1);
                bool hasPid = p.PIDNumber != null && p.PIDNumber.ToString()!.Trim().Length > 0;

                int newStatus;
                if (notExists3or2)
                {
                    newStatus = 10;
                }
                else if (exists2or1)
                {
                    newStatus = 9;
                }
                else if (hasPid)
                {
                    newStatus = 8;
                }
                else
                {
                    newStatus = 7;
                }

                if (p.ProjectStatus != newStatus)
                {
                    try
                    {
                        await _projectsContainer.PatchItemAsync<ProjectDocument>(
                            p.id,
                            new PartitionKey(p.id),
                            new[] { PatchOperation.Set("/ProjectStatus", newStatus) },
                            cancellationToken: ct);

                        changedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to patch project status for project '{ProjectId}' (id: '{Id}').", projId, p.id);
                        throw;
                    }
                }
            }

            return changedCount;
        }
    }
}
