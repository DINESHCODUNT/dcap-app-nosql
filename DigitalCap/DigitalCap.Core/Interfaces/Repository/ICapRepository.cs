using System;
using System.Collections.Generic;
using System.Text;
using DigitalCap.Core.Models;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface ICapRepository
    {
        Task<List<ProjectsGrid>> GetCapIndexAsync();
        Task<List<ProjectCharts>> GetProjectByStatusesAsync();
        Task<List<ProjectCharts>> GetCertsByRegionAsync();
        Task<List<ProjectChartsCount>> GetProjectCountAsync();
        Task<List<ProjectChartsCount>> GetCertsCountAsync();
    }
}
