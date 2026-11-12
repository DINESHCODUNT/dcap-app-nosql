using DigitalCap.Core.Models.ReportConfig;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IProjectReportService
    {
        Task<ProjectReport> GetReport(int projectId, int partId, int imonumber, List<string> sectionIds);
    }
}
