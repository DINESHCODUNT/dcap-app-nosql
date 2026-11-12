using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class ReportAPIService : IReportAPIService
    {
        private readonly IReportService _reportService;

        public ReportAPIService(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<ServiceResult<byte[]>> ExportAllProjectsAsExcelSpreadsheet()
        {
            try
            {
                var result = await _reportService.ExportAllProjectsAsExcelSpreadsheet();
                return ServiceResult<byte[]>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<byte[]>.Failure("Failed to export projects as Excel spreadsheet");
            }
        }

        public async Task<ServiceResult<byte[]>> ExportAllProjectsGridAsExcelSpreadsheet()
        {
            try
            {
                var result = await _reportService.ExportAllProjectsGridAsExcelSpreadsheet();
                return ServiceResult<byte[]>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<byte[]>.Failure("Failed to export projects grid as Excel spreadsheet");
            }
        }

        public async Task<ServiceResult<List<Dictionary<string, string>>>> ExportProjectsPartial(int minRange, int maxRange)
        {
            try
            {
                var result = await _reportService.ExportProjectsPartial(minRange, maxRange);
                return ServiceResult<List<Dictionary<string, string>>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Dictionary<string, string>>>.Failure("Failed to export projects partial");
            }
        }
    }

}