using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Service
{
    public class CapService : ICapService
    {
        private readonly ICapRepository _capRepository;
        private readonly ILogger<CapService> _logger;

        public CapService(
            ICapRepository capRepository,
            ILogger<CapService> logger)
        {
            _capRepository = capRepository;
            _logger = logger;
        }

        public async Task<ServiceResult<List<ProjectsGrid>>> GetCapIndexAsync()
        {
            try
            {
                var list = await _capRepository.GetCapIndexAsync();

                return ServiceResult<List<ProjectsGrid>>
                    .SuccessResult(list ?? new List<ProjectsGrid>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching CAP Index");

                return ServiceResult<List<ProjectsGrid>>
                    .Failure("An error occurred while fetching CAP Index.");
            }
        }

        public async Task<ServiceResult<List<ProjectCharts>>> GetProjectByStatusesAsync()
        {
            try
            {
                var result = await _capRepository.GetProjectByStatusesAsync();

                if (result == null || !result.Any())
                {
                    return ServiceResult<List<ProjectCharts>>.Failure("No project status data found.");
                }

                return ServiceResult<List<ProjectCharts>>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching project statuses");

                return ServiceResult<List<ProjectCharts>>
                    .Failure("An error occurred while fetching project statuses.");
            }
        }

        public async Task<ServiceResult<List<ProjectCharts>>> GetCertsByRegionAsync()
        {
            try
            {
                var result = await _capRepository.GetCertsByRegionAsync();

                return ServiceResult<List<ProjectCharts>>
                    .SuccessResult(result ?? new List<ProjectCharts>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching certs by region");

                return ServiceResult<List<ProjectCharts>>
                    .Failure("An error occurred while fetching certs by region.");
            }
        }

        public async Task<ServiceResult<List<ProjectChartsCount>>> GetProjectCountAsync()
        {
            try
            {
                var result = await _capRepository.GetProjectCountAsync();

                return ServiceResult<List<ProjectChartsCount>>
                .SuccessResult(result ?? new List<ProjectChartsCount>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching project count");

                return ServiceResult<List<ProjectChartsCount>>
                    .Failure("An error occurred while fetching project count.");
            }
        }

        public async Task<ServiceResult<List<ProjectChartsCount>>> GetCertsCountAsync()
        {
            try
            {
                var result = await _capRepository.GetCertsCountAsync();

                return ServiceResult<List<ProjectChartsCount>>
                    .SuccessResult(result ?? new List<ProjectChartsCount>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching certs count");

                return ServiceResult<List<ProjectChartsCount>>
                    .Failure("An error occurred while fetching certs count.");
            }
        }
    }
}
