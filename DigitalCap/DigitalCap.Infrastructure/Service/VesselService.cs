using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.Models.VesselModel;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class VesselService : IVesselService
    {

        private readonly IFreedomAPIRepository _freedomAPIRepository;
        private readonly IVesselRepository _vesselRepository;
        private readonly ITankService _tankService;
        private readonly IClassService _classService;
        private readonly IFreedomAPIRepository _freedomApiRepository;
        private readonly IProjectRepository _projectRepository;

        public VesselService(
            IFreedomAPIRepository freedomAPIRepository,
            IVesselRepository vesselRepository,
            ITankService tankService,
            IClassService classService,
            IFreedomAPIRepository freedomApiRepository,
            IProjectRepository projectRepository)
        {
            _freedomAPIRepository = freedomAPIRepository;
            _vesselRepository = vesselRepository;
            _tankService = tankService;
            _classService = classService;
            _freedomApiRepository = freedomApiRepository;
            _projectRepository = projectRepository;
        }

        public Task<ServiceResult<string>> CreateStatutoryCertificate(int projectId, Report report)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResult<string>> CreateSurveyAudit(SurveyStatus surveyStatus, int projectId)
        {
            throw new NotImplementedException();
        }

        public async Task<ServiceResult<string>> CreateVesselMainData(string classNumber, Project project)
        {
            try
            {
                Core.Models.VesselModel.Vessel vessel = null;

                if (!string.IsNullOrWhiteSpace(classNumber))
                {
                    vessel = await _freedomAPIRepository.GetVessel(classNumber);
                }

                await _vesselRepository.CreateVesselMainDataAsync(classNumber, project, vessel);

                return ServiceResult<string>.Success(classNumber);
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Failure($"Error creating vessel main data: {ex.Message}");
            }
        }

        public async Task<ServiceResult<Core.Models.Vessel>> Details(string imoNumber)
        {
            var result = await _vesselRepository.Details(imoNumber);

            return ServiceResult<Core.Models.Vessel>.Success(result);
        }

        public async Task<ServiceResult<VesselDetails>> LookUp([FromQuery] string classNumber)
        {
            var vessel = new DigitalCap.Core.Models.Vessel();
            var vesselDetails = _freedomApiRepository.GetVessel(classNumber).Result;
            if (vesselDetails != null)
            {
                vessel = DigitalCap.Core.Models.Vessel.MapVessel(vesselDetails);
            }

            var viewModel = new VesselDetails
            {
                AbsClassID = Convert.ToInt32(vessel.ClassNum) == 0
                                ? null
                                : vessel.ClassNum.ToString(),
                Builder = vessel.Builder,
                ClassSociety = await _classService.MapFromName(vessel.ClassedBy, vessel.ClassedBy2),
                HullNumber = vessel.HullNum,
                LengthOverall = vessel.Length,
                MonthBuilt = vessel.BuildDate?.Month,
                YearBuilt = vessel.BuildDate?.Year,
                VesselName = vessel.VesselName,
                IMO = vessel.IMONum.ToString()
            };

            var shipTypes = await _tankService.GetShipType();
            var value = shipTypes.Data.Find(x => x.VesselType == vessel.VesselType);

            if (value != null)
            {
                viewModel.Type = value.Id;
            }

            return ServiceResult<VesselDetails>.Success(viewModel);
        }
        public async Task<ServiceResult<object>> LookUpByImo([FromQuery] int imoNumber)
        {
            VesselDetails vessel = new();
            var result = await _projectRepository.CheckProjectExistsByImoNumber(imoNumber);
            var projectnamelist = (await _projectRepository.GetProjectListByIMO(imoNumber)).OrderByDescending(x => x.Value).FirstOrDefault();
            if (projectnamelist != null)
            {
                var projectdetails = await _projectRepository.GetProject(Convert.ToInt32(projectnamelist.Value));

                vessel = new VesselDetails
                {
                    AbsClassID = Convert.ToInt32(projectdetails.AbsClassID != "" ? projectdetails.AbsClassID : 0) == 0
                                   ? null
                                   : projectdetails.AbsClassID.ToString(),
                    Builder = projectdetails.Builder,
                    ClassSociety = projectdetails.ClassSociety,
                    HullNumber = projectdetails.HullNumber ?? projectdetails.HullNumber,
                    LengthOverall = Convert.ToDouble(projectdetails.LengthOverall != null ? projectdetails.LengthOverall : 0),
                    MonthBuilt = projectdetails.MonthBuilt,
                    YearBuilt = projectdetails.YearBuilt,
                    VesselName = projectdetails.VesselName,
                    IMO = projectdetails.IMO.ToString()

                };

                var value = _tankService.GetShipType().Result.Data.Find(x => x.VesselType == projectdetails.VesselType);
                if (value != null)
                    vessel.Type = value.Id;
            }

            return ServiceResult<object>.Success(new { result = result, project = projectnamelist, vesseldetails = vessel });
        }
    }
}
