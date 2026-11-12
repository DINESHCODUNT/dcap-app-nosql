using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vessel = DigitalCap.Core.Models.Vessel;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IVesselService
    {
        Task<ServiceResult<string>> CreateSurveyAudit(SurveyStatus surveyStatus, int projectId);
        Task<ServiceResult<string>> CreateStatutoryCertificate(int projectId, Report report);
        Task<ServiceResult<Core.Models.Vessel>> Details(string imoNumber);
        Task<ServiceResult<string>> CreateVesselMainData(string classNumber, Project project);

        Task<ServiceResult<VesselDetails>> LookUp(string classNumber);
        Task<ServiceResult<object>> LookUpByImo(int imoNumber);
    }
}
