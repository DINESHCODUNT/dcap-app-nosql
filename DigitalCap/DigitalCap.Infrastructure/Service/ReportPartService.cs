using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class ReportPartService : IReportPartService
    {
        private readonly IReportPartRepository _reportPartRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IVesselRepository _vesselRepository;
        private readonly ISurveyReportRepository _surveyReportRepository;
        private readonly ITankRepository _tankRepository;
        private readonly ISecurityService _securityService;
        public ReportPartService(IReportPartRepository reportPartRepository, IProjectRepository projectRepository
            , IVesselRepository vesselRepository, ISurveyReportRepository surveyReportRepository,
            ITankRepository tankRepository, ISecurityService securityService)
        {
            _reportPartRepository = reportPartRepository;
            _projectRepository = projectRepository;
            _vesselRepository = vesselRepository;
            _surveyReportRepository = surveyReportRepository;
            _tankRepository = tankRepository;
            _securityService = securityService;
        }

        public async Task<ServiceResult<ReportConfigDropDownDataList>> GetReportPartsByVesselType(int vesselTypeId)
        {
            var result = await _reportPartRepository.GetReportPartsByVesselTypeAsync(vesselTypeId);

            return ServiceResult<ReportConfigDropDownDataList>.Success(result);
        }

        public async Task<ServiceResult<PartSectionNamesList>> GetSectionNamesByPartNameAsync(int vesselTypeId, string partName)
        {
            var result = await _reportPartRepository.GetSectionNamesByPartNameAsync(vesselTypeId, partName);

            return ServiceResult<PartSectionNamesList>.Success(result);
        }
        public async Task<ServiceResult<PartSectionNamesList>> GetSectionNamesByPartIdAsync(int vesselTypeId, int partId)
        {
            var result = await _reportPartRepository.GetSectionNamesByPartId(vesselTypeId, partId);

            return ServiceResult<PartSectionNamesList>.Success(result);
        }
        public async Task<ServiceResult<bool>> CreateReportPart(VesselTypeReportConfigList reportConfigList, int vesselTypeId, string modifiedBy)
        {
            var result = await _reportPartRepository.CreateReportPart(reportConfigList, vesselTypeId, modifiedBy);

            return ServiceResult<bool>.Success(result);
        }
        public async Task<ServiceResult<ReportConfigDropDownDataList>> GetReportPartsByVesselTypeAsync(int vesselTypeId)
        {
            var result = await _reportPartRepository.GetReportPartsByVesselTypeAsync(vesselTypeId);

            return ServiceResult<ReportConfigDropDownDataList>.Success(result);
        }
        public async Task<ServiceResult<ProjectReport>> GetReportBySectionId(int projectId, int partId, Guid? SectionId, int imoNumber, List<string> SectionIds)
        {
            try
            {
                var retApp = new ProjectReport();

                retApp.ProjectName = await _projectRepository.GetProjectName(projectId);
                retApp.ProjectVesselType = _projectRepository.GetProjectVesselType(projectId).Result;

                retApp.Vessel = new Core.Models.Survey.Vessel();
                var ReportVessel = new ReportVesselMainData();
                //Get CERTIFICATE USING CLASSNUMBER
                try
                {
                    ReportVessel = _vesselRepository.GetVesselMainData(projectId).Result.First();
                }
                catch (Exception ex)
                { }
                if (ReportVessel != null)
                {
                    retApp.ReportPort = ReportVessel.reportPort;
                    retApp.ReportDate = ReportVessel.portDate != null && Convert.ToDateTime(ReportVessel.portDate) != DateTime.MinValue ? Convert.ToDateTime(ReportVessel.portDate).ToShortDateString() : DateTime.Now.ToString("dd-MM-yyy");

                    retApp.ReportNo = ReportVessel.reportNo;
                    retApp.Vessel.ReportNo = ReportVessel.reportNo;
                    if (ReportVessel.actualReportStartDate != null && Convert.ToDateTime(ReportVessel.actualReportStartDate).Date != DateTime.MinValue.Date)
                    {
                        retApp.Vessel.ActualReportStartDate = Convert.ToDateTime(ReportVessel.actualReportStartDate);
                    }
                    retApp.ReportPort = ReportVessel.reportPort;
                    retApp.Vessel.Classed = ReportVessel.classed;
                    retApp.Vessel.VesselName = ReportVessel.vesselName;
                    retApp.Vessel.VesselID = ReportVessel.vesselID;
                    retApp.Vessel.ImoNumber = ReportVessel.imoNumber;
                    retApp.Vessel.VesselType = ReportVessel.vesselType;
                    retApp.Vessel.YearBuilt = Convert.ToInt32(ReportVessel.yearBuilt);
                    retApp.Vessel.BuiltBy = ReportVessel.builtBy;
                    retApp.Vessel.HullNo = ReportVessel.hullNo;
                    retApp.Vessel.FlagName = ReportVessel.flagName;
                    retApp.Vessel.Homeport = ReportVessel.homeport;
                    retApp.Vessel.OfficalNumber = ReportVessel.officalNumber;
                    retApp.Vessel.CallSign = ReportVessel.callSign;
                    retApp.Vessel.PreviousName = ReportVessel.previousName;
                    retApp.Vessel.OwnerName = ReportVessel.ownerName;
                    retApp.Vessel.Manager = ReportVessel.manager;
                    retApp.Vessel.Length = ReportVessel.length != null ? float.Parse(ReportVessel.length) : 0;
                    retApp.Vessel.Breadth = ReportVessel.breadth != null ? float.Parse(ReportVessel.breadth) : 0;
                    retApp.Vessel.DEPTH = ReportVessel.depth != null ? float.Parse(ReportVessel.depth) : 0;
                    retApp.Vessel.Draft = ReportVessel.draft != null ? float.Parse(ReportVessel.draft) : 0;
                    retApp.Vessel.DEADWEIGHT = ReportVessel.deadweight != null ? ReportVessel.deadweight : null;
                    retApp.Vessel.GrossTons = ReportVessel.grossTons != null ? float.Parse(ReportVessel.grossTons) : 0;
                    retApp.Vessel.NetTons = ReportVessel.netTons != null ? float.Parse(ReportVessel.netTons) : 0;
                    retApp.Vessel.PropulsionType = ReportVessel.propulsionType;
                    retApp.Vessel.KW = ReportVessel.kw != null ? ReportVessel.kw : "";
                    retApp.Vessel.ShaftRPM = ReportVessel.shaftRPM != null ? float.Parse(ReportVessel.shaftRPM) : 0;
                    retApp.Vessel.PropulsionManufacturer = ReportVessel.propulsionManufacturer;
                    retApp.Vessel.GearManufacturer = ReportVessel.gearManufacturer;
                    retApp.Vessel.Classed = ReportVessel.classed;
                    retApp.Vessel.ClassNo = ReportVessel.classNo;
                    retApp.Vessel.ClassNotation = ReportVessel.classNotation;
                    retApp.Vessel.Machinery = ReportVessel.machinery;
                    retApp.Vessel.Other = ReportVessel.other;
                    retApp.Vessel.Environment = ReportVessel.environment;
                    retApp.Vessel.SelectedPrefix = ReportVessel.selectedPrefix.ToString();
                    retApp.Vessel.FirstVisitDate = ReportVessel.firstVisitDate;

                    retApp.Vessel.LastVisitDate = ReportVessel.lastVisitDate;

                }

                retApp.SurveyStatus = _vesselRepository.GetSurveyStatus(projectId).Result.FirstOrDefault();
                var certificates = _vesselRepository.GetStatutoryCertificate(projectId).Result;
                retApp.StatutoryCertificatesExpirationDates = certificates.StatutoryCertificatesExpirationDates;
                retApp.StatutoryCertificatesIssuedDates = certificates.StatutoryCertificatesIssuedDates;
                retApp.ProjectId = projectId;
                var projectydetails = _projectRepository.GetProject(projectId).Result;
                var currentCondition = _surveyReportRepository.GetCurrentCondition().Result;
                var gradingcondition = _reportPartRepository.GetGradingCondition().Result;
                ////********GetReportTemplate*******
                retApp.ProjectStatus = projectydetails.ProjectStatus;
                ProjectView newReport = new ProjectView();
                ProjectReportMapping reportnotes = new ProjectReportMapping();
                List<Guid> reportSections = new List<Guid>();
                var templateDetails = _reportPartRepository.GetProjectTemplateTitle(partId, projectId).Result;
                newReport.Id = templateDetails.PartId;
                newReport.ProjectId = projectId;
                newReport.TemplateTitle = templateDetails.PartName;
                /*Handling Normal Sections*/
                var normaSectionData = new ProjectSpecificSections();
                var normalsections = _reportPartRepository.GetNormalSectionByProjectId(projectId, partId).Result;
                if (normalsections != null && normalsections?.Count() > 0)
                {
                    normalsections = normalsections.OrderBy(x => x.Order).ToList();

                    foreach (var section in normalsections)
                    {
                        reportSections.Add(section.SectionMappingId);

                    }

                    if (SectionId != null && SectionId != Guid.Empty)
                        normalsections = normalsections.Where(x => x.SectionMappingId == SectionId).ToList();



                    var gradingconditions = _reportPartRepository.GetGradingConditionByProjectId(projectId).Result;
                    normaSectionData.subSections = new List<Core.Models.ReportConfig.ProjectSubSection>();
                    foreach (var section in normalsections)
                    {
                        reportnotes = _surveyReportRepository.GetProjectReportTemplate(projectId, partId, section.SectionMappingId)?.Result;
                        if (reportnotes != null && reportnotes.Notes != null)
                        {
                            section.SectionNotes = reportnotes.Notes;
                        }


                        section.GradingLabelName = new List<string>() { "1", "2", "3", "4", "5", "N/A" };
                        section.Grading = _reportPartRepository.GetGradingBySectionId(projectId, section.SectionMappingId).Result;
                        section.currentConditions = currentCondition;
                        section.imageDescriptions = _reportPartRepository.GetImageDescriptionsBySectionId(projectId, section.SectionMappingId).Result;

                        section.ImageCards = new List<GenericImageCard>();
                        section.ImageCards = _reportPartRepository.GetGenericImageCard(projectId, partId, section.SectionMappingId).Result;


                        if (section.Grading != null && section.Grading.Count > 0)
                        {
                            var gradingnotes = _surveyReportRepository.GetProjectGrading(projectId, partId, section.SectionMappingId)?.Result;
                            foreach (var grading in section.Grading.Where(x => x.RequiredInReport == true).ToList())
                            {
                                grading.Checkbox = new List<ProjectCheckBox>();
                                grading.Checkbox = _reportPartRepository.GetGradingCondition().Result;
                                grading?.Checkbox?.ForEach(x => { x.GradingId = grading.Id; x.SectionId = section.SectionMappingId; });

                                if (section.GradingLabelName.Count < grading.Checkbox.Count)
                                {
                                    section.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();
                                }
                                if (grading.Checkbox != null && grading.Checkbox.Count > 0)
                                {

                                    var gradingvalue = gradingconditions?.Where(x => x.SectionId == section.SectionMappingId && x.GradingId == grading.Id)?.ToList();
                                    if (gradingvalue != null && gradingvalue?.Count > 0)
                                    {
                                        foreach (var gradingcheckbox in grading.Checkbox)
                                        {
                                            var val = gradingvalue.Where(x => x.Name == gradingcheckbox.Name).Select(x => x.Value).FirstOrDefault();
                                            if (val)
                                            {
                                                gradingcheckbox.Value = val;

                                            }
                                        }
                                    }
                                }
                                if (gradingnotes != null && gradingnotes.Count > 0)
                                {
                                    grading.Value = gradingnotes.Where(x => x.GradingId == grading.Id).Select(x => x.Value).FirstOrDefault();
                                }
                            }


                        }
                        normaSectionData.subSections.Add(section);
                        if (section.HasSubsection)
                        {
                            /*Handling SubSection Sections*/
                            var subsections = _reportPartRepository.GetProjectSubsectionBySectionId(section.SectionMappingId).Result;
                            foreach (var subsection in subsections)
                            {
                                section.imageDescriptions?.AddRange(_reportPartRepository.GetImageDescriptionsBySubSectionId(projectId, subsection.SectionMappingId).Result);
                                subsection.Order = section.Order;
                                subsection.IsSubSection = true;
                                if (section.ImageCards == null)
                                {
                                    section.ImageCards = new List<GenericImageCard>();
                                }
                                section.ImageCards.AddRange(_reportPartRepository.GetGenericImageCard(projectId, partId, subsection.SectionMappingId).Result);


                            }
                        }
                    }


                    newReport.Sections = new ProjectSpecificSections();
                    newReport.Sections = normaSectionData;
                    newReport?.Sections?.subSections.OrderBy(x => x.Order);
                }


                /*Handling Tank sections*/


                var tanksections = _reportPartRepository.GetTankSectionByProjectId(projectId, partId, imoNumber).Result;

                if (SectionId != null && SectionId != Guid.Empty)
                {
                    if (tanksections != null && tanksections?.Count() > 0)
                    {
                        reportSections.AddRange(tanksections?.Select(x => x.Id).ToList());
                        tanksections = tanksections.Where(x => x.Id == SectionId).ToList();
                    }
                }

                if (tanksections != null && tanksections?.Count() > 0)
                {

                    retApp.tankReportView = new ProjectTankReportView();
                    retApp.tankReportView.ProjectId = projectId;
                    retApp.tankReportView.TemplateId = partId;
                    retApp.tankReportView.TemplateName = templateDetails.PartName.Replace("Part ", String.Empty);
                    retApp.tankReportView.sectionStartCount = normalsections != null ? normalsections.Count() + 1 : 1;
                    retApp.tankReportView.Sections = new List<ProjectTankUI>();
                    retApp.tankReportView.VesselType = projectydetails.VesselType;
                    retApp.tankReportView.Sections = tanksections;

                    /*Get Image Description*/
                    foreach (var tanksection in retApp.tankReportView.Sections)
                    {
                        tanksection.GradingLabelName = new List<string>() { "1", "2", "3", "4", "5", "N/A" };
                        tanksection.Grading = await _tankRepository.GetTemplateTankGrading(tanksection.TankTypeId, projectId, projectydetails.VesselType);
                        tanksection.currentConditions = currentCondition;
                        tanksection.imageDescriptions = _reportPartRepository.GetImageDescriptionsByTankTypeId(projectId, tanksection.TankTypeId).Result;
                        var tankreportnotes = await _surveyReportRepository.GetProjectReportTemplate(projectId, partId, tanksection.Id);
                        if (tankreportnotes != null && tankreportnotes.Notes != null)
                        {
                            tanksection.SectionNotes = tankreportnotes.Notes;
                        }
                        if (tanksection.Grading != null && tanksection.Grading.Count > 0)
                        {

                            foreach (var grading in tanksection.Grading.Where(x => x.RequiredInReport).ToList())
                            {

                                var templategradingnotes = await _surveyReportRepository.GetProjectGrading(projectId, partId, tanksection.Id);
                                grading.Checkbox = new List<TankCheckBox>();
                                grading.Checkbox = await _tankRepository.GetTemplateTankGradingCondition(grading.Id);
                                if (tanksection.GradingLabelName.Count < grading.Checkbox.Count)
                                {
                                    tanksection.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();
                                }
                                var templategradingconditions = await _surveyReportRepository.GetProjectGradingConditionMapping(projectId, partId, tanksection.Id, grading.Id);

                                if (templategradingnotes != null && templategradingnotes.Count > 0)
                                {
                                    grading.Value = templategradingnotes.Where(x => x.GradingId == grading.Id).Select(x => x.Value).FirstOrDefault();
                                }
                                if (grading.Checkbox != null && grading.Checkbox.Count > 0)
                                {
                                    if (templategradingconditions != null && templategradingconditions.Count > 0)
                                    {
                                        foreach (var gradingcheckbox in grading.Checkbox)
                                        {
                                            gradingcheckbox.Value = templategradingconditions.Where(x => x.GradingConditionLabel == gradingcheckbox.Name).Select(x => x.GradingConditionValue).FirstOrDefault();
                                        }
                                    }
                                }
                            }
                        }
                        tanksection.tankImageCards = new List<TankImageCard>();
                        tanksection.tankImageCards = await _tankRepository.GetProjectTankImageCard(projectId, partId, tanksection.Id);

                    }
                }
                var sectionIdGroup = new List<ProjectGradingSection>();


                /*Handling H&I Template*/
                /*Handling H&I Template*/
                if (templateDetails.PartName == "Part H&I")
                {
                    newReport.handireport = new HandIReport();
                    List<ProjectGradingConditionMapping> gradingconditions = new List<ProjectGradingConditionMapping>();
                    newReport.handireport = _surveyReportRepository.GetHIReport(projectId).Result;
                    if (newReport.handireport == null)
                    {
                        newReport.handireport = new HandIReport();
                    }
                    newReport.handireport.Grading = _reportPartRepository.GetH_IGrading(retApp.ProjectVesselType == "Gas Carrier" ? true : false).Result;


                    var gradingCondition = _reportPartRepository.GetGradingCondition().Result;
                    newReport.handireport.sectionOptions = new List<Options>() { new Options { Label = "Yes", Value = "Yes" }, new Options { Label = "No", Value = "No" }, new Options { Label = "Others", Value = "Others" } };
                    newReport.handireport.maintanenceOptions = new List<Options>() { new Options { Label = "Yes", Value = "Yes" }, new Options { Label = "No", Value = "No" }, new Options { Label = "N/A", Value = "N/A" } };

                    newReport.handireport.GradingLabelName = new List<String>();


                    if (gradingCondition != null && gradingCondition.Count > 0)
                    {
                        var handigradingconditions = _reportPartRepository.GetHandIGradingConditionByProjectId(projectId)?.Result;
                        var gradingnotes = _reportPartRepository.GetH_IProjectGradingByProjectId(projectId)?.Result;
                        foreach (var grading in newReport.handireport.Grading)
                        {

                            grading.Checkbox = gradingCondition.Select(x => new CheckBox { Id = x.Id, Name = x.Name }).ToList();
                            if (newReport.handireport.GradingLabelName.Count < grading.Checkbox.Count)
                            {
                                newReport.handireport.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();
                            }
                            if (grading.Checkbox != null && grading.Checkbox.Count > 0)
                            {
                                if (handigradingconditions != null && handigradingconditions.Count > 0)
                                {
                                    foreach (var gradingcheckbox in grading.Checkbox)
                                    {
                                        gradingcheckbox.Value = handigradingconditions.Where(x => x.GradingId == grading.Id && x.GradingConditionLabel == gradingcheckbox.Name).Select(x => x.GradingConditionValue).FirstOrDefault();
                                    }
                                }
                            }
                            if (gradingnotes != null && gradingnotes.Count > 0)
                            {
                                grading.Value = gradingnotes.Where(x => x.GradingId == grading.Id).Select(x => x.Value).FirstOrDefault();
                            }
                        }

                    }

                }
                var tankorder = 0;

                if (newReport.Sections != null && newReport.Sections?.subSections?.Count > 0)
                {
                    tankorder = Convert.ToInt32(newReport.Sections?.subSections?.Count);
                    sectionIdGroup.AddRange(newReport.Sections.subSections.Select(x => new ProjectGradingSection { SectionId = x.SectionMappingId, SectionName = x.SectionName, Order = x.Order, IsSubSection = x.IsSubSection }).ToList());

                }
                if (retApp.tankReportView?.Sections != null && retApp.tankReportView?.Sections?.Count > 0)
                {
                    sectionIdGroup.AddRange(retApp.tankReportView?.Sections?.Select(x => new ProjectGradingSection { SectionId = x.Id, SectionName = x.TankName, Order = tankorder + 1 }).ToList());
                }


                retApp.sections = sectionIdGroup.Where(x => x.SectionId == SectionId).ToList();
                retApp.sectionIds = reportSections;
                retApp.template = newReport;

                return ServiceResult<ProjectReport>.Success(retApp);
            }

            catch (Exception ex)
            {
                throw ex;
            }

        }

    }
}
