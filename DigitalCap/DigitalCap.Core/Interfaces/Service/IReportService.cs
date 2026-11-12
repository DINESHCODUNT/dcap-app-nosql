using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IReportService
    {
        Task<byte[]> ExportAllProjectsAsExcelSpreadsheet();
        Task<byte[]> ExportAllProjectsGridAsExcelSpreadsheet();
        Task<List<Dictionary<string, string>>> ExportProjectsPartial(int minRange, int maxRange);
        Task<List<Dictionary<string, string>>> ExportAllProjectsForUserAsync(string userName);

    }
}
