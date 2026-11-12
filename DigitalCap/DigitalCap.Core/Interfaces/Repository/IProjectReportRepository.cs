using DigitalCap.Core.Models.ReportConfig;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IProjectReportRepository
    {
        Task<GenericImageCard> GetGenericImagCardByName(int projectId, int templateId, Guid sectionId, int cardNumber);
        Task<bool> UpdateGenericImageCard(GenericImageCard model);
        Task<bool> CreateGenericImageCard(GenericImageCard model);
        Task<List<ProjectPart>> GetProjectTemplatebyProjectId(int projectId);
    }
}
