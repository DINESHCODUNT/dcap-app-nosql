using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface ICapService
    {
        Task<ServiceResult<List<ProjectsGrid>>> GetCapIndexAsync();
        Task<ServiceResult<List<ProjectCharts>>>GetProjectByStatusesAsync();
        Task<ServiceResult<List<ProjectCharts>>> GetCertsByRegionAsync();
        Task<ServiceResult<List<ProjectChartsCount>>> GetProjectCountAsync();
        Task<ServiceResult<List<ProjectChartsCount>>> GetCertsCountAsync();

    }
}
