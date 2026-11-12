using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.VesselModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IVesselRepository
    {
        Task<IEnumerable<SurveyStatus>> GetSurveyStatus(int projectId);
        Task<IEnumerable<ReportVesselMainData>> GetVesselMainData(int projectId);
        Task<Core.Models.Survey.Certificates> GetStatutoryCertificate(int projectId);
        Task<bool> UpdateStatutoryCertificate(int projectId, Core.Models.Survey.Report report);
        Task<bool> UpdateSurveyAudit(Core.Models.Survey.SurveyStatus surveyStatus, int projectId);

        Task<Core.Models.Vessel?> Details(string imoNumber);
        Task<bool> UpdateReportMainDataAsync(Report report, int projectId);
        Task<string> CreateVesselMainDataAsync(string classNumber, Project project);
        Task CreateVesselMainDataAsync(string classNumber, Project project, Core.Models.VesselModel.Vessel vessel);
    }
}
