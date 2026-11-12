using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IReportAPIService
    {
        Task<ServiceResult<byte[]>> ExportAllProjectsAsExcelSpreadsheet();
        Task<ServiceResult<byte[]>> ExportAllProjectsGridAsExcelSpreadsheet();
        Task<ServiceResult<List<Dictionary<string, string>>>> ExportProjectsPartial(int minRange, int maxRange);

    }
}
