using DigitalCap.Core.Models.Reporting;
using DigitalCap.Core.Models.ExportConfig;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IAsposePdfService
    {
        Aspose.Pdf.Document GenerateFromHtml(string htmlContent);
        Task<(byte[], string)> GenerateCAPReportPdf(int projectId);
        Task<(byte[], string)> GeneratePartReportPdf(int projectId, int templateId, List<ExportPart> selectedSettings = null);
        Task<(byte[], string)> GenerateHIReportPdf(int projectId, int templateId);
        Task<(byte[], string)> GenerateFullReportPdf(int projectId, string currentUserName, int? templateSectionId, List<ExportPart> selectedSettings = null);
    }
}
