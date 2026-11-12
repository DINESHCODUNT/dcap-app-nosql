using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IReportRepository
    {
        Task<byte[]> ExportAllProjectsAsExcelSpreadsheet();
        Task<byte[]> ExportAllProjectsGridAsExcelSpreadsheet();
        Task<List<Dictionary<string, string>>> ExportProjectsPartial(int minRange, int maxRange);

    }
}
