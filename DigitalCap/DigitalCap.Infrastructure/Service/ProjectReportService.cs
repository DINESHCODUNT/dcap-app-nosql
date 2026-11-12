using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class ProjectReportService : IProjectReportService
    {
        private readonly IVesselRepository _vesselRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ITankRepository _tankRepository;
        private readonly ISurveyReportRepository _surveyReportRepository;
        private readonly IReportPartRepository _reportPartRepository;
        public ProjectReportService(IVesselRepository vesselRepository, IProjectRepository projectRepository, ITankRepository tankRepository
            , ISurveyReportRepository surveyReportRepository, IReportPartRepository reportPartRepository)
        {
            _vesselRepository = vesselRepository;
            _projectRepository = projectRepository;
            _tankRepository = tankRepository;
            _surveyReportRepository = surveyReportRepository;
            _reportPartRepository = reportPartRepository;
        }
        public async Task<ProjectReport> GetReport(int projectId, int partId, int imonumber, List<string> sectionIds)
        {

            try
            {
                var retApp = new ProjectReport();

                retApp.ProjectName = await _projectRepository.GetProjectName(projectId);
                retApp.ProjectVesselType = await _projectRepository.GetProjectVesselType(projectId); // was .Result
                retApp.Vessel = new DigitalCap.Core.Models.Survey.Vessel() { ImoNumber = imonumber };

                var reportVessel = new ReportVesselMainData { imoNumber = imonumber };
                try
                {
                    reportVessel = (await _vesselRepository.GetVesselMainData(projectId)).First(); // was .Result.First()
                }
                catch { /* vessel lookup is optional */ }

                if (reportVessel != null)
                {
                    retApp.ReportPort = reportVessel.reportPort;
                    retApp.ReportDate = reportVessel.portDate != null
                                      && Convert.ToDateTime(reportVessel.portDate) != DateTime.MinValue
                                          ? Convert.ToDateTime(reportVessel.portDate).ToShortDateString()
                                          : DateTime.Now.ToString("dd-MM-yyy");
                    retApp.ReportNo = reportVessel.reportNo;
                    retApp.Vessel.ReportNo = reportVessel.reportNo;

                    if (reportVessel.actualReportStartDate != null
                        && Convert.ToDateTime(reportVessel.actualReportStartDate).Date != DateTime.MinValue.Date)
                        retApp.Vessel.ActualReportStartDate = Convert.ToDateTime(reportVessel.actualReportStartDate);

                    retApp.Vessel.Classed = reportVessel.classed;
                    retApp.Vessel.VesselName = reportVessel.vesselName;
                    retApp.Vessel.VesselID = reportVessel.vesselID;
                    retApp.Vessel.ImoNumber = reportVessel.imoNumber != 0 ? reportVessel.imoNumber : imonumber;
                    retApp.Vessel.VesselType = reportVessel.vesselType;
                    retApp.Vessel.YearBuilt = Convert.ToInt32(reportVessel.yearBuilt);
                    retApp.Vessel.BuiltBy = reportVessel.builtBy;
                    retApp.Vessel.HullNo = reportVessel.hullNo;
                    retApp.Vessel.FlagName = reportVessel.flagName;
                    retApp.Vessel.Homeport = reportVessel.homeport;
                    retApp.Vessel.OfficalNumber = reportVessel.officalNumber;
                    retApp.Vessel.CallSign = reportVessel.callSign;
                    retApp.Vessel.PreviousName = reportVessel.previousName;
                    retApp.Vessel.OwnerName = reportVessel.ownerName;
                    retApp.Vessel.Manager = reportVessel.manager;
                    retApp.Vessel.Length = reportVessel.length != null ? float.Parse(reportVessel.length) : 0;
                    retApp.Vessel.Breadth = reportVessel.breadth != null ? float.Parse(reportVessel.breadth) : 0;
                    retApp.Vessel.DEPTH = reportVessel.depth != null ? float.Parse(reportVessel.depth) : 0;
                    retApp.Vessel.Draft = reportVessel.draft != null ? float.Parse(reportVessel.draft) : 0;
                    retApp.Vessel.DEADWEIGHT = reportVessel.deadweight;
                    retApp.Vessel.GrossTons = reportVessel.grossTons != null ? float.Parse(reportVessel.grossTons) : 0;
                    retApp.Vessel.NetTons = reportVessel.netTons != null ? float.Parse(reportVessel.netTons) : 0;
                    retApp.Vessel.PropulsionType = reportVessel.propulsionType;
                    retApp.Vessel.KW = reportVessel.kw ?? string.Empty;
                    retApp.Vessel.ShaftRPM = reportVessel.shaftRPM != null ? float.Parse(reportVessel.shaftRPM) : 0;
                    retApp.Vessel.PropulsionManufacturer = reportVessel.propulsionManufacturer;
                    retApp.Vessel.GearManufacturer = reportVessel.gearManufacturer;
                    retApp.Vessel.ClassNo = reportVessel.classNo;
                    retApp.Vessel.ClassNotation = reportVessel.classNotation;
                    retApp.Vessel.Machinery = reportVessel.machinery;
                    retApp.Vessel.Other = reportVessel.other;
                    retApp.Vessel.Environment = reportVessel.environment;
                    retApp.Vessel.SelectedPrefix = reportVessel.selectedPrefix.ToString();
                    retApp.Vessel.FirstVisitDate = reportVessel.firstVisitDate;
                    retApp.Vessel.LastVisitDate = reportVessel.lastVisitDate;
                }

                //  Parallel top-level lookups 
                var surveyStatusTask = _vesselRepository.GetSurveyStatus(projectId);
                var certificatesTask = _vesselRepository.GetStatutoryCertificate(projectId);
                var projectDetailsTask = _projectRepository.GetProject(projectId);
                var currentCondTask = _surveyReportRepository.GetCurrentCondition();
                var gradingCondTask = _reportPartRepository.GetGradingCondition();
                var templateDetailsTask = _reportPartRepository.GetProjectTemplateTitle(partId, projectId);
                var normalSectionsTask = _reportPartRepository.GetNormalSectionByProjectId(projectId, partId);

                await Task.WhenAll(
                    surveyStatusTask, certificatesTask, projectDetailsTask,
                    currentCondTask, gradingCondTask, templateDetailsTask, normalSectionsTask);

                var certificates = await certificatesTask;
                var projectDetails = await projectDetailsTask;
                var currentCond = await currentCondTask;
                var gradingCond = await gradingCondTask;
                var templateDetails = await templateDetailsTask;
                var normalSections = await normalSectionsTask;

                if (templateDetails == null)
                {
                    templateDetails = new DigitalCap.Core.Models.ReportConfig.ProjectPart
                    {
                        PartId = partId,
                        PartName = partId == 0 ? "Vessel Main Data" : string.Empty
                    };
                }

                retApp.SurveyStatus = (await surveyStatusTask).FirstOrDefault();
                retApp.StatutoryCertificatesExpirationDates = certificates.StatutoryCertificatesExpirationDates;
                retApp.StatutoryCertificatesIssuedDates = certificates.StatutoryCertificatesIssuedDates;
                retApp.ProjectId = projectId;
                retApp.ProjectStatus = projectDetails.ProjectStatus;

                List<Guid> reportSectionIds = new List<Guid>();
                var sectionIdGroup = new List<ProjectGradingSection>();
                ProjectView newReport = new ProjectView
                {
                    Id = templateDetails.PartId,
                    ProjectId = projectId,
                    TemplateTitle = templateDetails.PartName
                };

                // Normal sections 
                int sectionOrder = 0;
                var normaSectionData = new ProjectSpecificSections();
                if (normalSections?.Any() == true)
                {
                    var gradingConditions = await _reportPartRepository.GetGradingConditionByProjectId(projectId);
                    normaSectionData.subSections = new List<ProjectSubSection>();

                    foreach (var section in normalSections.OrderBy(x => x.Order))
                    {
                        sectionOrder++;
                        reportSectionIds.Add(section.SectionMappingId);

                        var reportNotes = await _surveyReportRepository.GetProjectReportTemplate(projectId, partId, section.SectionMappingId);
                        if (reportNotes?.Notes != null)
                            section.SectionNotes = reportNotes.Notes;

                        section.GradingLabelName = new List<string> { "1", "2", "3", "4", "5", "N/A" };
                        section.Grading = await _reportPartRepository.GetGradingBySectionId(projectId, section.SectionMappingId);
                        section.currentConditions = currentCond;
                        section.imageDescriptions = await _reportPartRepository.GetImageDescriptionsBySectionId(projectId, section.SectionMappingId);
                        section.ImageCards = await _reportPartRepository.GetGenericImageCard(projectId, partId, section.SectionMappingId);
                        // Get existing image cards from DB
                        section.ImageCards = await _reportPartRepository.GetGenericImageCard(
                            projectId,
                            partId,
                            section.SectionMappingId
                        ) ?? new List<GenericImageCard>();

                        // Total cards to generate
                        int totalCards = section.Totalcards;

                        // Number of placeholder cards
                        int placeholderCount = section.PlaceholderCount;

                        // Example:
                        // PartName = "Part N"
                        // CardName = N1-1, N2-2 ... N50-50

                        var cardPrefix = templateDetails.PartName?.Trim();

                        if (!string.IsNullOrWhiteSpace(cardPrefix))
                        {
                            // "Part N" -> "N"
                            cardPrefix = System.Text.RegularExpressions.Regex.Replace(
                                cardPrefix,
                                @"^Part\s*",
                                "",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                            ).Trim();
                        }
                        else
                        {
                            cardPrefix = string.Empty;
                        }

                        // Existing card numbers
                        var existingCardNumbers = section.ImageCards
                            .Where(x => x.CardNumber > 0)
                            .Select(x => x.CardNumber)
                            .ToHashSet();

                        // Generate cards from 1 to Totalcards
                        for (int cardNumber = 1; cardNumber <= totalCards; cardNumber++)
                        {
                            // Already exists in DB
                            var existingCard = section.ImageCards
                                .FirstOrDefault(x => x.CardNumber == cardNumber);

                            if (existingCard != null)
                            {
                                // Make sure existing CardName follows required format
                                existingCard.CardName = $"{cardPrefix}{sectionOrder}-{cardNumber}";

                                continue;
                            }

                            // New card
                            section.ImageCards.Add(new GenericImageCard
                            {
                                ProjectId = projectId,
                                SectionId = section.SectionMappingId,

                                // N1-1, N2-2 ... N50-50
                                CardName = $"{cardPrefix}{sectionOrder}-{cardNumber}",

                                CardNumber = cardNumber,

                                // First 40 = placeholder
                                // Remaining 10 = dropdown/current condition
                                CurrentCondition = cardNumber <= placeholderCount ? 0 : null,

                                IsActive = true,
                                IsDeleted = false,
                                IsSync = false
                            });
                        }

                        // Always order 1 -> Totalcards
                        section.ImageCards = section.ImageCards
                            .Where(x => x.CardNumber >= 1 && x.CardNumber <= totalCards)
                            .OrderBy(x => x.CardNumber)
                            .ToList();
                        await ApplyGradingStateAsync(section, projectId, partId, gradingConditions);

                        section.projectCards = BuildProjectCardsFromImageCards(
                            section.ImageCards, projectId, partId, section.SectionMappingId);

                        bool isPartF = normaSectionData.subSections.Any(x => x.HasSubsection);
                        section.Order = isPartF
                            ? Convert.ToInt32(normaSectionData.subSections.Count + section.Order)
                            : section.Order;

                        normaSectionData.subSections.Add(section);

                        if (section.HasSubsection)
                        {
                            var subsections = await _reportPartRepository.GetProjectSubsectionBySectionId(section.SectionMappingId);
                            foreach (var sub in subsections)
                            {
                                sectionOrder++;
                                reportSectionIds.Add(sub.SectionMappingId);
                                sub.IsSubSection = true;
                                sub.imageDescriptions = await _reportPartRepository.GetImageDescriptionsBySubSectionId(projectId, sub.SectionMappingId);
                                sub.ImageCards = await _reportPartRepository.GetGenericImageCard(projectId, partId, sub.SectionMappingId)
                                    ?? new List<GenericImageCard>();

                                EnsureGeneratedImageCards(sub, projectId, cardPrefix, sectionOrder);

                                sub.currentConditions = currentCond;
                                sub.GradingLabelName = new List<string> { "1", "2", "3", "4", "5", "N/A" };
                                sub.Grading = await _reportPartRepository.GetGradingBySectionId(projectId, sub.SectionMappingId);

                                var subNotes = await _surveyReportRepository.GetProjectReportTemplate(projectId, partId, sub.SectionMappingId);
                                if (subNotes?.Notes != null)
                                    sub.SectionNotes = subNotes.Notes;

                                await ApplyGradingStateAsync(sub, projectId, partId, gradingConditions);

                                sub.projectCards = BuildProjectCardsFromImageCards(
                                    sub.ImageCards, projectId, partId, sub.SectionMappingId);

                                normaSectionData.subSections.Add(sub);
                            }
                        }

                    }

                    newReport.Sections = normaSectionData;
                }

                // Tank sections 
                var tankSections = await _reportPartRepository.GetTankSectionByProjectId(projectId, partId, imonumber);
                if (tankSections?.Any() == true)
                {
                    reportSectionIds.AddRange(tankSections.Select(x => x.Id));

                    retApp.tankReportView = new ProjectTankReportView
                    {
                        ProjectId = projectId,
                        TemplateId = partId,
                        TemplateName = templateDetails.PartName.Replace("Part ", string.Empty),
                        sectionStartCount = (normalSections?.Count() ?? 0) + 1,
                        VesselType = projectDetails.VesselType,
                        Sections = tankSections
                    };

                    // Batch all shared lookups before the loop
                    var imageDescTask = _reportPartRepository.GetImageDescriptionsByTankTypeId(projectId, 0);
                    var notesTask = _surveyReportRepository.GetProjectReportTemplate(projectId, partId);
                    var imageCardsTask = _tankRepository.GetProjectTankImageCard(projectId, partId, Guid.Empty);
                    var gradingNotesTask = _surveyReportRepository.GetProjectGrading(projectId, partId, Guid.Empty);
                    var gradingCondMapTask = _surveyReportRepository.GetProjectGradingConditionMapping(projectId, partId, Guid.Empty, 0);

                    await Task.WhenAll(imageDescTask, notesTask, imageCardsTask, gradingNotesTask, gradingCondMapTask);

                    var imageDescriptions = await imageDescTask;
                    var sectionNotesList = await notesTask;
                    var imageCards = await imageCardsTask;
                    var gradingNotesList = await gradingNotesTask;
                    var gradingCondMap = await gradingCondMapTask;

                    // Step 1: assign Grading to all sections before building the condition task dictionary.
                    var tempGrading = await _tankRepository.GetTemplateTankGrading(0, projectId, projectDetails.VesselType);
                    foreach (var tankSection in tankSections)
                    {
                        tankSection.Grading = tempGrading
                            .Where(x => x.TankTypeId == tankSection.TankTypeId)
                            .Select(x => new TankGradingUI
                            {
                                Id = x.Id,
                                Name = x.Name,
                                TankTypeId = x.TankTypeId,
                                Value = x.Value,
                                SectionId = tankSection.Id
                            })
                            .ToList();

                        tankSection.GradingLabelName = new List<string>();
                    }

                    // Step 2: batch all GetTemplateTankGradingCondition calls in parallel.
                    // Keyed by (SectionId, GradingId) tuple — same as SurveyReportRepository.
                    var allGradingConditionTasks = tankSections
                        .Where(s => s.Grading?.Count > 0)
                        .SelectMany(s => s.Grading.Select(g => new { SectionId = s.Id, Grading = g }))
                        .ToDictionary(
                            x => (x.SectionId, x.Grading.Id),
                            x => _tankRepository.GetTemplateTankGradingCondition(x.Grading.Id));

                    await Task.WhenAll(allGradingConditionTasks.Values);

                    // Step 3: assign checkboxes, notes, and other per-section data.
                    foreach (var tankSection in tankSections)
                    {
                        sectionOrder++;
                        tankSection.currentConditions = currentCond;
                        tankSection.imageDescriptions = imageDescriptions
                            .Where(x => x.TankTypeId == tankSection.TankTypeId)
                            .Select(x => new ImageDescriptionUI { Id = x.Id, Description = x.Description })
                            .ToList();
                        tankSection.SectionNotes = sectionNotesList.FirstOrDefault(x => x.SectionId == tankSection.Id)?.Notes ?? string.Empty;
                        tankSection.tankImageCards = imageCards.Where(x => x.SectionId == tankSection.Id).ToList();


                        // Generate Tank Image Cards

                        var existingImageCards = imageCards
                            .Where(x => x.SectionId == tankSection.Id)
                            .OrderBy(x => x.CardNumber)
                            .ToList();


                        // "Part N" -> "N"
                        var cardPrefix = templateDetails.PartName?.Trim();

                        if (!string.IsNullOrWhiteSpace(cardPrefix))
                        {
                            cardPrefix = System.Text.RegularExpressions.Regex.Replace(
                                cardPrefix,
                                @"^Part\s*",
                                "",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                            ).Trim();
                        }
                        else
                        {
                            cardPrefix = string.Empty;
                        }


                        // THIS CHANGES FOR EACH TANK
                        //
                        // 1st tank -> N3
                        // 2nd tank -> N4
                        // 3rd tank -> N5

                        var cardNamePrefix = $"{cardPrefix}{sectionOrder}-";


                        var totalCards = tankSection.Totalcards;

                        var placeholderCount = tankSection.PlaceholderCount;

                        var tankImageCards = existingImageCards;


                        for (int cardNumber = 1; cardNumber <= totalCards; cardNumber++)
                        {
                            var existingCard = tankImageCards
                                .FirstOrDefault(x => x.CardNumber == cardNumber);

                            if (existingCard != null)
                            {
                                existingCard.CardName =
                                    $"{cardNamePrefix}{cardNumber}";

                                continue;
                            }

                            tankImageCards.Add(new TankImageCard
                            {
                                SectionId = tankSection.Id,

                                CardName = $"{cardNamePrefix}{cardNumber}",

                                CardNumber = cardNumber,

                                CurrentCondition = cardNumber <= placeholderCount ? 0 : null,

                                IsActive = true
                            });
                        }


                        tankSection.tankImageCards = tankImageCards
                            .Where(x =>
                                x.CardNumber >= 1 &&
                                x.CardNumber <= totalCards)
                            .OrderBy(x => x.CardNumber)
                            .ToList();

                        var gradingNotes = gradingNotesList?
                            .Where(x => x.SectionId == tankSection.Id)
                            .ToList()
                            ?? new List<ProjectGradingMapping>();

                        foreach (var grading in tankSection.Grading ?? Enumerable.Empty<TankGradingUI>())
                        {
                            var conditionMappings = gradingCondMap
                                .Where(x => x.SectionId == tankSection.Id && x.GradingId == grading.Id)
                                .ToList();

                            grading.Checkbox = allGradingConditionTasks
                                .TryGetValue((tankSection.Id, grading.Id), out var condTask)
                                    ? (await condTask)?.ToList() ?? new List<TankCheckBox>()
                                    : new List<TankCheckBox>();

                            if (tankSection.GradingLabelName.Count < grading.Checkbox.Count)
                                tankSection.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();

                            if (gradingNotes?.Count > 0)
                                grading.Value = gradingNotes.FirstOrDefault(x => x.GradingId == grading.Id)?.Value;

                            if (grading.Checkbox?.Count > 0 && conditionMappings.Count > 0)
                            {
                                foreach (var cb in grading.Checkbox)
                                {
                                    var match = conditionMappings.FirstOrDefault(x => x.GradingConditionLabel == cb.Name);
                                    if (match != null && match.GradingConditionValue != null)
                                        cb.Value = Convert.ToBoolean(match.GradingConditionValue);
                                }
                            }
                        }
                    }
                }

                // H&I template 
                if (templateDetails.PartName == "Part H&I")
                {
                    newReport.handireport = await _surveyReportRepository.GetHIReport(projectId)
                                         ?? new HandIReport();

                    newReport.handireport.Grading = await _reportPartRepository.GetH_IGrading(retApp.ProjectVesselType == "Gas Carrier");
                    newReport.handireport.sectionOptions = new List<Options>
                    {
                        new Options { Label = "Yes",    Value = "Yes" },
                        new Options { Label = "No",     Value = "No" },
                        new Options { Label = "Others", Value = "Others" }
                    };
                    newReport.handireport.maintanenceOptions = new List<Options>
                    {
                        new Options { Label = "Yes", Value = "Yes" },
                        new Options { Label = "No",  Value = "No" },
                        new Options { Label = "N/A", Value = "N/A" }
                    };
                    newReport.handireport.GradingLabelName = new List<string>();

                    if (gradingCond?.Count > 0)
                    {
                        var handiGradingConditions = await _reportPartRepository.GetHandIGradingConditionByProjectId(projectId);
                        var handiGradingNotes = await _reportPartRepository.GetH_IProjectGradingByProjectId(projectId);

                        foreach (var grading in newReport.handireport.Grading)
                        {
                            grading.Checkbox = gradingCond.Select(x => new CheckBox { Id = x.Id, Name = x.Name }).ToList();

                            if (newReport.handireport.GradingLabelName.Count < grading.Checkbox.Count)
                                newReport.handireport.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();

                            if (handiGradingConditions?.Count > 0)
                            {
                                foreach (var cb in grading.Checkbox)
                                {
                                    cb.Value = handiGradingConditions
                                        .Where(x => x.GradingId == grading.Id && x.GradingConditionLabel == cb.Name)
                                        .Select(x => x.GradingConditionValue)
                                        .FirstOrDefault();
                                }
                            }

                            if (handiGradingNotes?.Count > 0)
                                grading.Value = handiGradingNotes
                                    .Where(x => x.GradingId == grading.Id)
                                    .Select(x => x.Value)
                                    .FirstOrDefault();
                        }
                    }
                }

                // Assemble section ID groups 
                var tankOrder = 0;
                if (newReport.Sections?.subSections?.Count > 0)
                {
                    tankOrder = Convert.ToInt32(newReport.Sections.subSections.Max(x => x?.Order));
                    sectionIdGroup.AddRange(newReport.Sections.subSections.Select(x => new ProjectGradingSection
                    {
                        SectionId = x.SectionMappingId,
                        SectionName = x.SectionName,
                        Order = x.Order,
                        IsSubSection = x.IsSubSection,
                        TankTypeId = 0
                    }));
                }
                if (retApp.tankReportView?.Sections?.Count > 0)
                {
                    sectionIdGroup.AddRange(retApp.tankReportView.Sections.Select(x => new ProjectGradingSection
                    {
                        SectionId = x.Id,
                        SectionName = x.TankName,
                        Order = tankOrder + 1,
                        TankTypeId = x.TankTypeId
                    }));
                }

                retApp.sectionIds = reportSectionIds;
                retApp.sections = sectionIdGroup;
                retApp.ImoNumber = imonumber;
                retApp.template = newReport;

                return retApp;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static List<ProjectCardMapping> BuildProjectCardsFromImageCards(
            List<GenericImageCard> imageCards,
            int projectId,
            int partId,
            Guid sectionId)
        {
            if (imageCards == null || imageCards.Count == 0)
                return new List<ProjectCardMapping>();

            return imageCards
                .Where(c => c.ImageId > 0 || !string.IsNullOrEmpty(c.src))
                .Select(c => new ProjectCardMapping
                {
                    ProjectId = projectId,
                    TemplateId = partId,
                    SectionId = sectionId,
                    CardId = c.CardNumber > 0 ? c.CardNumber : c.Id,
                    DescriptionId = c.DescriptionId,
                    AdditionalDescription = c.AdditionalDescription,
                    CurrentCondition = c.CurrentCondition,
                    ImageId = c.ImageId,
                    Src = c.src
                })
                .ToList();
        }

        private static void EnsureGeneratedImageCards(
            ProjectSubSection section,
            int projectId,
            string cardPrefix,
            int sectionOrder)
        {
            section.ImageCards ??= new List<GenericImageCard>();

            var totalCards = section.Totalcards;
            var placeholderCount = section.PlaceholderCount;

            for (int cardNumber = 1; cardNumber <= totalCards; cardNumber++)
            {
                var existingCard = section.ImageCards.FirstOrDefault(x => x.CardNumber == cardNumber);
                if (existingCard != null)
                {
                    existingCard.CardName = $"{cardPrefix}{sectionOrder}-{cardNumber}";
                    continue;
                }

                section.ImageCards.Add(new GenericImageCard
                {
                    ProjectId = projectId,
                    SectionId = section.SectionMappingId,
                    CardName = $"{cardPrefix}{sectionOrder}-{cardNumber}",
                    CardNumber = cardNumber,
                    CurrentCondition = cardNumber <= placeholderCount ? 0 : null,
                    IsActive = true,
                    IsDeleted = false,
                    IsSync = false
                });
            }

            section.ImageCards = section.ImageCards
                .Where(x => x.CardNumber >= 1 && x.CardNumber <= totalCards)
                .OrderBy(x => x.CardNumber)
                .ToList();
        }

        private async Task ApplyGradingStateAsync(
            ProjectSubSection section,
            int projectId,
            int partId,
            List<ProjectCheckBox> gradingConditions)
        {
            if (section.Grading == null || section.Grading.Count == 0)
                return;

            section.GradingLabelName ??= new List<string> { "1", "2", "3", "4", "5", "N/A" };

            var gradingNotes = await _surveyReportRepository.GetProjectGrading(projectId, partId, section.SectionMappingId);
            foreach (var grading in section.Grading)
            {
                grading.Checkbox = grading.GradingConditionGroupId == 1
                    ? await _reportPartRepository.GetGradingCondition()
                    : new List<ProjectCheckBox>();

                grading.Checkbox.ForEach(x =>
                {
                    x.GradingId = grading.Id;
                    x.SectionId = section.SectionMappingId;
                });

                if (section.GradingLabelName.Count < grading.Checkbox.Count)
                    section.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();

                var gradingValues = gradingConditions?
                    .Where(x => x.SectionId == section.SectionMappingId && x.GradingId == grading.Id)
                    .ToList();

                if (gradingValues?.Count > 0)
                {
                    foreach (var cb in grading.Checkbox)
                    {
                        var val = gradingValues.Where(x => x.Name == cb.Name).Select(x => x.Value).FirstOrDefault();
                        if (val) cb.Value = val;
                    }
                }

                if (gradingNotes?.Count > 0)
                    grading.Value = gradingNotes.Where(x => x.GradingId == grading.Id).Select(x => x.Value).FirstOrDefault();
            }
        }

    }
}
