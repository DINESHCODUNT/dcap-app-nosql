using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Service
{
    /// <summary>MVC SurveyController validation and Part H&amp;I grading parity.</summary>
    public partial class ProjectSurveyService
    {
        public async Task<ServiceResult<string>> ValidateProjectGradingAsync(
            int projectId, int templateSectionId, int imo, List<ExportSections>? settings)
        {
            settings ??= new List<ExportSections>();
            try
            {
                var sectionIds = new List<string>();
                var reportParts = await GetSectionIdsByProjectIdAsync(projectId, templateSectionId == 10 ? null : templateSectionId, imo);
                if (templateSectionId != 10)
                {
                    var rp = reportParts.ReportPartGrid?.FirstOrDefault(x => x.PartId == templateSectionId);
                    sectionIds = rp?.SectionIds?.Select(x => x.ToString()!).ToList() ?? new List<string>();
                }

                var filename = string.Empty;
                if (templateSectionId != 1)
                {
                    sectionIds = sectionIds.Where(s => settings.Any(a => a.SectionId == Guid.Parse(s))).ToList();

                    var reportResult = await _reportPartService.GetReportBySectionId(projectId, templateSectionId, null, imo, sectionIds);
                    var ret = reportResult.Data;
                    if (ret == null || !reportResult.IsSuccess)
                        return ServiceResult<string>.Success(string.Empty);

                    var templateTitle = ret.template != null && !string.IsNullOrEmpty(ret.template.TemplateTitle)
                        ? ret.template.TemplateTitle
                        : string.Empty;

                    if (templateTitle != string.Empty && templateTitle != "Vessel Main Data")
                    {
                        var tankgradingfilename = string.Empty;

                        if (ret.template?.Sections?.subSections != null && ret.template.Sections.subSections.Count > 0)
                        {
                            var imagecardList = ret.template.Sections.subSections
                                .Where(x => settings.Any(a => a.SectionId == x.SectionMappingId)).ToList();

                            if (imagecardList.Select(p => p.Grading?.Count ?? 0).FirstOrDefault() > 0)
                            {
                                var gradingcount = 0;
                                var finalcount = 0;
                                var checkbox = 0;
                                imagecardList.All(p =>
                                {
                                    if (p?.Grading != null)
                                    {
                                        p.Grading = p.Grading.Where(x => x.RequiredInReport).ToList();
                                        gradingcount = p.Grading.Count;
                                        if (gradingcount > 0)
                                        {
                                            foreach (var g in p.Grading)
                                            {
                                                if (g.Checkbox?.Count > 0)
                                                {
                                                    foreach (var u in g.Checkbox)
                                                    {
                                                        if (u.Value && checkbox == 0)
                                                        {
                                                            checkbox++;
                                                            finalcount++;
                                                        }
                                                    }
                                                    checkbox = 0;
                                                }
                                                else
                                                {
                                                    gradingcount--;
                                                }
                                            }

                                            if (gradingcount != finalcount)
                                                tankgradingfilename += p.SectionName + ",";
                                        }
                                    }
                                    finalcount = 0;
                                    return true;
                                });
                            }
                        }

                        if (ret.tankReportView?.Sections != null)
                        {
                            var tankui = ret.tankReportView.Sections.Where(x => settings.Any(a => a.SectionId == x.Id)).ToList();
                            foreach (var p in tankui)
                            {
                                if (p.Grading == null)
                                    continue;
                                p.Grading = p.Grading.Where(x => x.RequiredInReport).ToList();
                                var tankgradingcount = p.Grading.Count;
                                var tankfinalcount = 0;
                                var tankcheckbox = 0;
                                foreach (var g in p.Grading)
                                {
                                    if (g.Checkbox != null)
                                    {
                                        foreach (var u in g.Checkbox)
                                        {
                                            if (u.Value && tankcheckbox == 0)
                                            {
                                                tankcheckbox++;
                                                tankfinalcount++;
                                            }
                                        }
                                        tankcheckbox = 0;
                                    }
                                }
                                if (tankgradingcount != tankfinalcount)
                                    tankgradingfilename += p.TankName + ",";
                            }
                        }

                        if (ret.template?.handireport?.Grading != null && ret.template.handireport.Grading.Count > 0)
                        {
                            var handireport = ret.template.handireport.Grading.ToList();
                            var hrGradingcount = handireport.Count;
                            var hrFinalcount = 0;
                            var hrCheckbox = 0;
                            foreach (var e in handireport)
                            {
                                if (e.Checkbox != null)
                                {
                                    foreach (var u in e.Checkbox)
                                    {
                                        if (u.Value && hrCheckbox == 0)
                                        {
                                            hrCheckbox++;
                                            hrFinalcount++;
                                        }
                                    }
                                    hrCheckbox = 0;
                                }
                            }
                            if (hrGradingcount != hrFinalcount)
                                tankgradingfilename += "Part H & I,";
                        }

                        tankgradingfilename = tankgradingfilename != string.Empty ? tankgradingfilename.Remove(tankgradingfilename.Length - 1) : string.Empty;
                        filename = tankgradingfilename;
                    }
                }

                var handIReport = reportParts.Report?.template?.handireport?.Grading;
                if (handIReport != null && handIReport.Count > 0)
                {
                    var handireport = handIReport.ToList();
                    var hrGradingcount = handireport.Count;
                    var hrFinalcount = 0;
                    var hrCheckbox = 0;
                    foreach (var e in handireport)
                    {
                        if (e.Checkbox != null)
                        {
                            foreach (var u in e.Checkbox)
                            {
                                if (u.Value && hrCheckbox == 0)
                                {
                                    hrCheckbox++;
                                    hrFinalcount++;
                                }
                            }
                            hrCheckbox = 0;
                        }
                    }
                    if (hrGradingcount != hrFinalcount)
                        filename += "Part H & I,";
                }

                string formattedResult = "##" + filename + "#";
                return ServiceResult<string>.Success(formattedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ValidateProjectGradingAsync failed");
                return ServiceResult<string>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<string>> ExportAllProjectValidationAsync(int projectId, int imo, List<ExportPart> settings)
        {
            var sectionIds = new List<string>();
            var allGrading = string.Empty;
            var allCurrentcondition = string.Empty;
            var allDescription = string.Empty;
            var allAdditionaldescription = string.Empty;

            try
            {
                var partIds = await _reportPartRepository.GetProjectExportPartConfiguration(projectId) ?? new List<ExportPart>();
                partIds = partIds.Where(s => settings.Any(a => a.PartId == s.PartId)).ToList();

                foreach (var element in partIds)
                {
                    var reportResult = await _reportPartService.GetReportBySectionId(projectId, element.PartId, null, imo, sectionIds);
                    var ret = reportResult.Data;
                    var grading = string.Empty;
                    var currentcondition = string.Empty;
                    var description = string.Empty;
                    var additionaldescription = string.Empty;

                    if (ret != null && !string.IsNullOrEmpty(element.PartName) && element.PartName != "Vessel Main Data")
                    {
                        var tankui = new List<ProjectTankUI>();
                        var tankimagecardList = new List<TankImageCard>();
                        var selectedSections = settings.Where(x => x.PartId == element.PartId).Select(x => x.sections.ToList()).FirstOrDefault();

                        if (selectedSections == null)
                            continue;

                        var projectui = new List<DigitalCap.Core.Models.ReportConfig.ProjectSubSection>();
                        if (ret.template?.Sections?.subSections != null && ret.template.Sections.subSections.Count > 0)
                        {
                            var imagecardList = ret.template.Sections.subSections
                                .Where(s => selectedSections.Any(a => a.SectionId == s.SectionMappingId)).ToList();

                            if (imagecardList != null && imagecardList.Select(p => p.Grading?.Count ?? 0).FirstOrDefault() > 0)
                            {
                                imagecardList.All(p =>
                                {
                                    var gradingcount = 0;
                                    var finalcount = 0;
                                    var checkbox = 0;
                                    p.Grading = p.Grading?.Where(x => x.RequiredInReport == true)?.ToList();
                                    if (p?.Grading?.Count > 0 && p.Grading != null)
                                    {
                                        gradingcount = p.Grading.Count;
                                        if (gradingcount > 0)
                                        {
                                            p.Grading.ForEach(g =>
                                            {
                                                if (g.Checkbox?.Count > 0)
                                                {
                                                    g.Checkbox.ForEach(u =>
                                                    {
                                                        if (u.Value && checkbox == 0)
                                                        {
                                                            checkbox++;
                                                            finalcount++;
                                                        }
                                                    });
                                                    checkbox = 0;
                                                }
                                                else
                                                {
                                                    gradingcount--;
                                                }
                                            });

                                            if (gradingcount != finalcount)
                                                grading += p.SectionName + ",";
                                        }
                                    }
                                    return true;
                                });
                            }

                            var imagecards = imagecardList?.Where(x => x.ImageCards != null).SelectMany(x => x.ImageCards!).ToList();

                            var totacardlist = new List<GenericImageCard>();
                            foreach (var tui in projectui)
                            {
                                var prjcardmap = tui.ImageCards != null
                                    ? tui.ImageCards.Where(x => x.CardNumber > tui.PlaceholderCount && x.CurrentCondition == 0 && (x.src != null || x.DescriptionId != 0)).ToList()
                                    : new List<GenericImageCard>();
                                totacardlist.AddRange(prjcardmap);
                            }
                            if (totacardlist.Count > 0)
                                currentcondition = string.Join(",", totacardlist.Select(a => a.CardName));

                            if (imagecards != null && imagecards.Count > 0)
                            {
                                var all2 = imagecards.Where(d => (d.DescriptionId == 0 && d.src != null) || (d.DescriptionId == 0 && d.CurrentCondition != 0)).ToList();
                                if (all2.Count > 0)
                                    description = string.Join(",", all2.Select(a => a.CardName));

                                var additionalDescId = imagecardList!.SelectMany(s => s.imageDescriptions ?? new List<ImageDescriptionUI>()).Where(s => s.Description == "Others").ToList();
                                var additionalDesc = imagecards.Where(d => d.DescriptionId != 0 && (d.AdditionalDescription == null || d.AdditionalDescription == string.Empty)).ToList();
                                var emptyAdditionaldesc = additionalDesc.Where(a => additionalDescId.Any(b => b.Id == a.DescriptionId)).ToList();
                                if (emptyAdditionaldesc.Count > 0)
                                    additionaldescription = string.Join(",", emptyAdditionaldesc.Select(a => a.CardName));
                            }
                        }

                        if (ret.tankReportView != null)
                        {
                            tankui = ret.tankReportView.Sections?.Where(s => selectedSections.Any(a => a.SectionId == s.Id)).ToList() ?? new List<ProjectTankUI>();

                            if (tankui.Select(p => p.Grading?.Count ?? 0).FirstOrDefault() > 0)
                            {
                                foreach (var p in tankui)
                                {
                                    p.Grading = p.Grading?.Where(x => x.RequiredInReport).ToList();
                                    if (p.Grading?.Count > 0)
                                    {
                                        var tankgradingcount = p.Grading.Count;
                                        var tankfinalcount = 0;
                                        var tankcheckbox = 0;
                                        foreach (var g in p.Grading)
                                        {
                                            if (g.Checkbox != null)
                                            {
                                                foreach (var u in g.Checkbox)
                                                {
                                                    if (u.Value && tankcheckbox == 0)
                                                    {
                                                        tankcheckbox++;
                                                        tankfinalcount++;
                                                    }
                                                }
                                                tankcheckbox = 0;
                                            }
                                        }
                                        if (tankgradingcount != tankfinalcount)
                                            grading += p.TankName + ",";
                                    }
                                }
                            }

                            var totaltanklist = new List<TankImageCard>();
                            foreach (var tui in tankui)
                            {
                                var tankimagecards = tui.tankImageCards;
                                if (tankimagecards != null && tankimagecards.Count > 0)
                                {
                                    var tanklistnew = tankimagecards.Where(x => x.CardNumber > tui.PlaceholderCount && x.CurrentCondition == 0 && (x.src != null || x.DescriptionId != 0)).ToList();
                                    totaltanklist.AddRange(tanklistnew);
                                }
                            }
                            if (totaltanklist.Count > 0)
                                currentcondition = currentcondition == string.Empty
                                    ? string.Join(",", totaltanklist.Select(a => a.CardName))
                                    : currentcondition + "," + string.Join(",", totaltanklist.Select(a => a.CardName));

                            tankimagecardList = ret.tankReportView.Sections?.Where(x => x.tankImageCards != null).SelectMany(x => x.tankImageCards!).ToList() ?? new List<TankImageCard>();

                            var descTanklist = tankimagecardList.Where(d => (d.DescriptionId == 0 && d.src != null) || (d.DescriptionId == 0 && d.CurrentCondition != 0)).ToList();
                            if (descTanklist.Count > 0)
                                description = description == string.Empty
                                    ? string.Join(",", descTanklist.Select(a => a.CardName))
                                    : description + "," + string.Join(",", descTanklist.Select(a => a.CardName));

                            var tankAdditionalDescId = tankui.SelectMany(s => s.imageDescriptions ?? new List<ImageDescriptionUI>()).Where(s => s.Description == "Others").ToList();
                            var tankAdditionalDesc = tankimagecardList.Where(d => d.DescriptionId != 0 && (d.AdditionalDescription == null || d.AdditionalDescription == string.Empty)).ToList();
                            var tankEmptyAdditionalDescriptionlist = tankAdditionalDesc.Where(a => tankAdditionalDescId.Any(b => b.Id == a.DescriptionId)).ToList();

                            if (tankEmptyAdditionalDescriptionlist.Count > 0)
                                additionaldescription = additionaldescription == string.Empty
                                    ? string.Join(",", tankEmptyAdditionalDescriptionlist.Select(a => a.CardName))
                                    : additionaldescription + "," + string.Join(",", tankEmptyAdditionalDescriptionlist.Select(a => a.CardName));
                        }

                        if (ret.template?.handireport?.Grading != null && ret.template.handireport.Grading.Count > 0)
                        {
                            var handireport = ret.template.handireport.Grading.ToList();
                            var hrGradingcount = handireport.Count;
                            var hrFinalcount = 0;
                            var hrCheckbox = 0;
                            foreach (var e in handireport)
                            {
                                if (e.Checkbox != null)
                                {
                                    foreach (var u in e.Checkbox)
                                    {
                                        if (u.Value && hrCheckbox == 0)
                                        {
                                            hrCheckbox++;
                                            hrFinalcount++;
                                        }
                                    }
                                    hrCheckbox = 0;
                                }
                            }
                            if (hrGradingcount != hrFinalcount)
                                grading += "Part H & I,";
                        }
                    }

                    grading = grading != string.Empty ? grading.Remove(grading.Length - 1) : string.Empty;

                    allGrading = grading != string.Empty
                        ? (allGrading == string.Empty ? grading : allGrading + "," + grading)
                        : allGrading;

                    allCurrentcondition = currentcondition != string.Empty
                        ? (allCurrentcondition == string.Empty ? currentcondition : allCurrentcondition + "," + currentcondition)
                        : allCurrentcondition;

                    allDescription = description != string.Empty
                        ? (allDescription == string.Empty ? description : allDescription + "," + description)
                        : allDescription;

                    allAdditionaldescription = additionaldescription != string.Empty
                        ? (allAdditionaldescription == string.Empty ? additionaldescription : allAdditionaldescription + "," + additionaldescription)
                        : allAdditionaldescription;
                }

                var result = allCurrentcondition + "#" + allDescription + "#" + allGrading + "#" + allAdditionaldescription;
                return ServiceResult<string>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportAllProjectValidationAsync failed");
                return ServiceResult<string>.Failure(ex.Message);
            }
        }

    }
}
