using DigitalCap.Persistence.Cosmos.Documents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public static class DynamicTaskStatus
    {
        public static int? Compute(
            ProjectTaskStatusDocument pts,
            IEnumerable<ProjectTaskStatusDocument> siblingsOfSameProject,
            ProjectDocument? project,
            DateTime utcNow)
        {
            if (pts.StatusId != 3)
            {
                return pts.StatusId;
            }

            switch (pts.TaskId)
            {
                case 5:
                    if (siblingsOfSameProject.Any(s => s.TaskId == 16 && s.StatusId == 1) ||
                        (project != null && SqlDate.DiffDays(project.CreatedDate ?? utcNow, utcNow) >= 21))
                    {
                        return 5;
                    }
                    return 3;

                case 6:
                    if (siblingsOfSameProject.Any(s => s.TaskId == 16 && s.StatusId == 1))
                    {
                        return 5;
                    }
                    return 3;

                case 10:
                case 11:
                    if (siblingsOfSameProject.Any(s => s.TaskId == 9 && s.StatusId == 1 && s.StatusDate.HasValue && SqlDate.DiffDays(s.StatusDate.Value, utcNow) >= 21))
                    {
                        return 5;
                    }
                    return 3;

                case 17:
                    if (siblingsOfSameProject.Any(s => s.TaskId == 16 && s.StatusId == 1 && s.StatusDate.HasValue && SqlDate.DiffDays(s.StatusDate.Value, utcNow) >= 120))
                    {
                        return 5;
                    }
                    return 3;

                case 19:
                    if (siblingsOfSameProject.Any(s => s.TaskId == 17 && s.StatusId == 1 && s.StatusDate.HasValue && SqlDate.DiffDays(s.StatusDate.Value, utcNow) >= 10))
                    {
                        return 5;
                    }
                    return 3;

                default:
                    return 3;
            }
        }

        public static int? ComputeDaysWorked(int? dynamicStatusId, DateTime? statusDate, DateTime? lastModifiedDate, DateTime utcNow)
        {
            if (dynamicStatusId == 3 || dynamicStatusId == 4)
            {
                return 0;
            }

            if (dynamicStatusId == 1)
            {
                if (!statusDate.HasValue)
                {
                    return null;
                }

                return SqlDate.DiffDays(statusDate.Value, lastModifiedDate ?? utcNow);
            }

            return SqlDate.DiffDays(statusDate ?? utcNow, utcNow);
        }
    }
}
