using Aspose.Pdf;
using Aspose.Pdf.Text;
using DigitalCap.Core.Helpers;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.ExportConfig;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class AsposePdfService : IAsposePdfService
    {
        private readonly ISurveyReportService _surveyReportService;
        private readonly IReportPartService _reportPartService;
        private readonly IReportPartRepository _reportPartRepository;
        private readonly ISurveyReportRepository _surveyReportRepository;
        private readonly IProjectReportService _projectReportService;
        private readonly ILogger<AsposePdfService> _logger;

        public AsposePdfService(ISurveyReportService surveyReportService, IReportPartService reportPartService, IReportPartRepository reportPartRepository, ISurveyReportRepository surveyReportRepository, IProjectReportService projectReportService, ILogger<AsposePdfService> logger)
        {
            _surveyReportService = surveyReportService;
            _reportPartService = reportPartService;
            _reportPartRepository = reportPartRepository;
            _surveyReportRepository = surveyReportRepository;
            _projectReportService = projectReportService;
            _logger = logger;
        }

                private string ReadEmbeddedResource(string fileName)
        {
            var assembly = typeof(AsposePdfService).Assembly;
            var resourceName = "DigitalCap.Infrastructure.Templates.ReportTemplate." + fileName;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) throw new FileNotFoundException("Embedded resource " + resourceName + " not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private void SetLicense()
        {
            var assembly = typeof(AsposePdfService).Assembly;
            using var stream = assembly.GetManifestResourceStream("DigitalCap.Infrastructure.Templates.Aspose.Total.Product.Family.lic");
            if (stream != null)
            {
                new Aspose.Pdf.License().SetLicense(stream);
                stream.Position = 0;
                new Aspose.Words.License().SetLicense(stream);
            }
        }

        public Aspose.Pdf.Document GenerateFromHtml(string htmlContent)
        {
            var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(htmlContent));
            var options = new HtmlLoadOptions
            {
                PageInfo = new PageInfo
                {
                    Width = PageSize.A4.Width,
                    Height = PageSize.A4.Height,
                    Margin = new MarginInfo(40, 80, 40, 60)
                },
                IsEmbedFonts = true,
                InputEncoding = "UTF-8"
            };
            var doc = new Document(htmlStream, options);
            htmlStream.Dispose();
            return doc;
        }

        public async Task<(byte[], string)> GenerateCAPReportPdf(int projectId)
        {
            try
            {
                SetLicense();

                var report = await _surveyReportService.GetReport(projectId, 0, new List<string>());

                var vmdHtml = BuildVesselMainDataHtml(report);
                var surveyHtml = BuildSurveyStatusHtml(report);
                var statHtml = BuildStatutoryHtml(report);
                var EvaluateHtml = BuildGradingHtml();

                var finalDoc = new Aspose.Pdf.Document();

                foreach (var html in new[] { vmdHtml, surveyHtml, statHtml, EvaluateHtml })
                {
                    var partDoc = GenerateFromHtml(html);
                    finalDoc.Pages.Add(partDoc.Pages);
                }

                using var stream = new MemoryStream();
                finalDoc.Save(stream);

                var vesselName = report?.Vessel?.VesselName ?? "CAP_Report";

                return (stream.ToArray(), $"{vesselName}_Report");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error generating CAP report PDF for projectId: {ProjectId}",
                    projectId);

                return (Array.Empty<byte>(), string.Empty);
            }
        }
        private string BuildVesselMainDataHtml(Report report)
        {
            var html = ReadEmbeddedResource("VesselMainData.html");
            var v = report?.Vessel ?? new Vessel();

            return html
                .Replace("{{VESSEL_NAME}}", v.VesselName ?? "")
                .Replace("{{NAME_PREFIX}}", v.SelectedPrefix switch
                {
                    "1" => "M/V",
                    "2" => "SS",
                    _ => v.SelectedPrefix ?? ""
                })
                .Replace("{{FLAG}}", v.FlagName ?? "")
                .Replace("{{IMO_NUMBER}}", v.ImoNumber.ToString() ?? "")
                .Replace("{{REPORT_NO}}", report?.ReportNo ?? "")
                .Replace("{{REPORT_PORT}}", report?.ReportPort ?? "")
                .Replace("{{PORT_VISIT_DATE}}", FormatVisitDate(v?.LastVisitDate))
                .Replace("{{FIRST_VISIT_DATE}}", FormatVisitDate(v?.FirstVisitDate))
                .Replace("{{VESSEL_TYPE}}", v.VesselType ?? "")
                .Replace("{{YEAR_BUILT}}", v.YearBuilt.ToString())
                .Replace("{{BUILT_BY}}", v.BuiltBy ?? "")
                .Replace("{{HULL_NO}}", v.HullNo ?? "")
                .Replace("{{PORT_OF_REGISTRY}}", v.Homeport ?? "")
                .Replace("{{OFFICIAL_NO}}", v.OfficalNumber ?? "")
                .Replace("{{CALL_SIGN}}", v.CallSign ?? "")
                .Replace("{{CLASSED}}", v.Classed ?? "")
                .Replace("{{CLASS_NO}}", v.ClassNo ?? "")
                .Replace("{{CLASS_NOTATIONS}}", v.ClassNotation ?? "")
                .Replace("{{ADDITIONAL_CLASS_NOTATIONS}}", v.Machinery ?? "")
                .Replace("{{OTHER_NOTATIONS}}", v.Other ?? "")
                .Replace("{{PREVIOUS_NAMES}}", v.PreviousName ?? "")
                .Replace("{{OWNERS}}", v.OwnerName ?? "")
                .Replace("{{MANAGERS}}", v.Manager ?? "")
                .Replace("{{LENGTH}}", v.Length.ToString("F2"))
                .Replace("{{BREADTH}}", v.Breadth.ToString("F2"))
                .Replace("{{DEPTH}}", v.DEPTH.ToString("F2"))
                .Replace("{{SUMMER_DRAFT}}", v.Draft.ToString("F2"))
                .Replace("{{DEADWEIGHT}}", v.DEADWEIGHT ?? "")
                .Replace("{{GROSS_TONS}}", v.GrossTons.ToString("F2"))
                .Replace("{{NET_TONS}}", v.NetTons.ToString("F2"))
                .Replace("{{PROPULSION_TYPE}}", v.PropulsionType ?? "")
                .Replace("{{PROPULSION_VALUE}}", v.ShaftRPM.ToString())
                .Replace("{{KW}}", v.KW ?? "")
                .Replace("{{PROPULSION_MANUFACTURER}}", v.PropulsionManufacturer ?? "")
                .Replace("{{GEAR_MANUFACTURER}}", v.GearManufacturer ?? "");
        }

        private string FormatReportDate(DateTime? d)
        {
            if (!d.HasValue || !NotApplicableDate.HasValue(d.Value))
                return "";
            if (d.Value.Date == NotApplicableDate.Value.Date)
                return NotApplicableDate.Text;
            return d.Value.ToString("dd-MMM-yyyy");
        }

        private string FormatVisitDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var trimmed = raw.Trim();
            if (trimmed == "01-01-1900" || trimmed == "1900-01-01" || NotApplicableDate.IsText(trimmed))
                return NotApplicableDate.Text;

            if (DateTime.TryParseExact(trimmed, "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var vd))
            {
                return vd.Date == NotApplicableDate.Value.Date ? NotApplicableDate.Text : vd.ToString("dd-MMM-yyyy");
            }

            if (DateTime.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed))
            {
                if (parsed.Date == NotApplicableDate.Value.Date) return NotApplicableDate.Text;
                if (!NotApplicableDate.HasValue(parsed)) return "";
                return parsed.ToString("dd-MMM-yyyy");
            }

            return trimmed;
        }

        private string BuildSurveyStatusHtml(Report report)
        {
            var html = ReadEmbeddedResource("SurveyStatus.html");
            var s = report?.SurveyStatus ?? new SurveyStatus();

            return html
                .Replace("{{VESSEL_NAME}}", report?.Vessel?.VesselName ?? "")
                .Replace("{{CLASS_SURVEY_DATE}}", FormatReportDate(s.ClassSurveyDate))
                .Replace("{{DRYDOCK_DATE}}", FormatReportDate(s.DryDockDate))
                .Replace("{{SPECIAL_HULL_NO}}", s.SpecialHullNo?.ToString() ?? "")
                .Replace("{{DUE_DATE}}", FormatReportDate(s.GetHullSurveyDate()))
                .Replace("{{MACHINERY_DATE}}", FormatReportDate(s.SpecialContinuousMachineryDate))
                .Replace("{{BOILER_DATE}}", FormatReportDate(s.BoilerDate))
                .Replace("{{TAILSHAFT_DATE}}", FormatReportDate(s.TailshaftDate));
        }

        private string BuildStatutoryHtml(Report report)
        {
            var html = ReadEmbeddedResource("StatutoryCertificates.html");
            var exp = report?.StatutoryCertificatesExpirationDates ?? new StatutoryCertificates();
            var iss = report?.StatutoryCertificatesIssuedDates ?? new StatutoryCertificates();

            var sb = new StringBuilder();
            string Row(string label, DateTime? date)
            {
                if (!date.HasValue || !NotApplicableDate.HasValue(date.Value))
                {
                    return $"<tr><th>{label}</th><td></td></tr>";
                }
                if (date.Value.Date == NotApplicableDate.Value.Date)
                {
                    return $"<tr><th>{label}</th><td>{NotApplicableDate.Text}</td></tr>";
                }
                var status = GetDateStatus(date);
                return $"<tr><th>{label}</th><td class=\"{status.css}\">{date.Value.ToString("dd-MMM-yyyy")} ({status.text})</td></tr>";
            }

            sb.Append("<table>");
            sb.Append(Row("Loadline Certificate Valid To", exp.LLLIDate));
            sb.Append(Row("Safety Construction Certificate Valid To", exp.SafetyConDate));
            sb.Append(Row("Safety Equipment Certificate Valid To", exp.SafetyEquipmentDate));
            sb.Append(Row("Safety Radio Certificate Valid To", exp.SafetyRadioDate));
            sb.Append(Row("IOPP Certificate Valid To", exp.IOPPDate));
            sb.Append(Row("IAPP Certificate Valid To", exp.IAPDate));
            sb.Append(Row("ISPP (Sewage) Certificate Valid To", exp.ISPPSewageDate));
            sb.Append(Row("IEE Certificate Issued Date", iss.IEEDate));
            sb.Append(Row("Cargo Gear Annual Certificate", iss.CargoGearAnnualCertificate));
            sb.Append(Row("Cargo Gear Retesting Certificate Valid To", exp.CargoGearReTestDate));
            sb.Append(Row("IAFS Certificate Issued Date", iss.IAFSDate));
            sb.Append(Row("IBWM Certificate Valid To", exp.IBWMDate));
            sb.Append(Row("ISM Certificate Valid To", exp.ISMDate));
            sb.Append(Row("ISPS Certificate Valid To", exp.ISPSDate));
            sb.Append(Row("Maritime Labour Certificate Valid To", exp.MaritimeLabourDate));
            sb.Append("</table>");

            return html.Replace("{{CERT_SECTIONS_HTML}}", sb.ToString());
        }

        private string BuildGradingHtml()
        {
            var html = ReadEmbeddedResource("VesselEvaluationGrading.html");

            var sb = new StringBuilder();

            string Row(string grade, string description)
            {
                return $@"
        <tr>
            <td>{grade}</td>
            <td>{description}</td>
        </tr>";
            }

            sb.Append("<table>");

            sb.Append(Row(
                "Grade 1",
                @"Condition superior.
Items examined and/or measured and found with only
superficial reductions from as new or current rule
scantlings. No maintenance or repair required."
            ));

            sb.Append(Row(
                "Grade 2",
                @"Condition above average.
Items examined and/or measured and found to have
deficiencies of minor nature not requiring correction or
repairs and/or found to have thickness significantly above
Class limits."
            ));

            sb.Append(Row(
                "Grade 3",
                @"Condition average.
Items examined and/or measured and either found to have
a deficiency which does not require immediate corrective
action, or found to have thicknesses, although generally
above Class renewal levels, with substantial corrosion."
            ));

            sb.Append(Row(
                "Grade 4",
                @"Condition below average.
Items examined and/or measured and either found to have
a deficiency or deficiencies which may affect the ship's
potential to remain in Class, or found to have, in some
areas, thicknesses, which are below the Class renewal levels."
            ));

            sb.Append(Row(
                "Grade 5",
                @"Conditions which could not be determined due to many
factors (tank not gas free, etc.)"
            ));

            sb.Append("</table>");

            var paragraph = @"
    <p>
        To arrive at an unbiased evaluation of the vessel's condition,
        the survey was carried out using this check sheet together
        with a grading system ranging from very good to unsatisfactory
        with the survey being done by surveyors acting independently
        of each other. Upon completion of CAP Survey by the attending
        CAP Surveyors, a grade was agreed upon and assigned to each
        item with results reported herein.
    </p>";

            return html
                .Replace("{{GRADING_TABLE}}", sb.ToString())
                .Replace("{{GRADING_DESCRIPTION}}", paragraph);
        }

        private (string css, string text) GetDateStatus(DateTime? date)
        {
            if (!date.HasValue) return ("", "-");
            var diff = (date.Value - DateTime.Today).TotalDays;
            if (diff < 0) return ("status-expired", "Expired");
            if (diff <= 90) return ("status-expiring", "Expiring Soon");
            return ("status-valid", "Valid");
        }

        public async Task<(byte[], string)> GeneratePartReportPdf(int projectId, int templateId, List<ExportPart> selectedSettings = null)
        {
            try
            {
                SetLicense();

                List<string> sectionIds = new List<string>();
                if (selectedSettings != null && selectedSettings.Any())
                {
                    var partSettings = selectedSettings.FirstOrDefault(p => p.PartId == templateId);
                    if (partSettings != null && partSettings.sections != null)
                    {
                        sectionIds = partSettings.sections.Select(s => s.SectionId.ToString()).ToList();
                    }
                }

                var report = await _surveyReportService.GetReport(projectId, templateId, sectionIds);

                // Fallback for dynamic templates (e.g. Part B / 1718) which return 0 sections from the legacy GetReport
                bool usedDynReportForTanks = false;
                if ((report?.template?.Sections == null || report.template.Sections.Count == 0) && (report?.tankReportView?.Sections == null || report.tankReportView.Sections.Count == 0) && templateId != 0 && selectedSettings != null)
                {
                    usedDynReportForTanks = true;
                    if (report == null) report = new Core.Models.Survey.Report { template = new Core.Models.Survey.View() };
                    if (report.template == null) report.template = new Core.Models.Survey.View();
                    report.template.Sections = new List<Sections>();
                    
                    int imo = report.Vessel?.ImoNumber ?? 0;
                    var dynReport = await _projectReportService.GetReport(projectId, templateId, imo, sectionIds);
                    
                    if (dynReport?.template?.Sections?.subSections != null)
                    {
                        var dynTemplate = dynReport.template;
                        var grouped = dynTemplate.Sections.subSections
                            .Where(s => sectionIds.Contains(s.SectionMappingId.ToString()))
                            .GroupBy(s => s.SectionName ?? "");
                        
                        foreach(var group in grouped)
                        {
                            var newSec = new Sections { 
                                SectionName = group.Key, 
                                subSections = new List<SubSection>() 
                            };
                            
                            foreach(var sub in group)
                            {
                                var newSub = new SubSection {
                                    Id = sub.Id,
                                    SubSectionName = sub.SubHeader,
                                    SectionNotes = sub.SectionNotes,
                                    ImageCards = sub.ImageCards?.Select(c => new Core.Models.Survey.ImageCards {
                                        Id = c.CardNumber > 0 ? c.CardNumber : c.Id,
                                        FileName = c.CardName
                                    }).ToList() ?? new List<Core.Models.Survey.ImageCards>(),
                                    imageDescriptions = sub.imageDescriptions ?? new List<ImageDescriptionUI>(),
                                    projectCards = sub.projectCards ?? new List<ProjectCardMapping>(),
                                    GradingLabelName = sub.GradingLabelName ?? new List<string>(),
                                    Grading = sub.Grading?.Select(g => new Core.Models.Survey.Grading {
                                        Name = g.Name,
                                        Value = g.Value,
                                        Checkbox = g.Checkbox?.Select(c => new Core.Models.Survey.CheckBox { Name = c.Name, Value = c.Value }).ToList() ?? new List<Core.Models.Survey.CheckBox>()
                                    }).ToList() ?? new List<Core.Models.Survey.Grading>()
                                };
                                newSec.subSections.Add(newSub);
                            }
                            report.template.Sections.Add(newSec);
                        }
                    }

                    if (dynReport?.tankReportView?.Sections != null)
                    {
                        // The sectionIds list (from Export Modal) only contains static sections, 
                        // so filtering dynamic tanks by sectionIds accidentally excludes them all.
                        var tankSections = dynReport.tankReportView.Sections
                            .GroupBy(t => t.TankName ?? "");

                        foreach (var group in tankSections)
                        {
                            var newSec = new Sections { 
                                SectionName = group.Key, 
                                subSections = new List<SubSection>() 
                            };
                            
                            foreach (var t in group)
                            {
                                var newSub = new SubSection {
                                    Id = t.Id,
                                    SubSectionName = t.Subheader ?? t.TankName,
                                    SectionNotes = t.SectionNotes,
                                    ImageCards = t.tankImageCards?.Select(c => new Core.Models.Survey.ImageCards {
                                        Id = c.CardNumber > 0 ? c.CardNumber : c.Id,
                                        FileName = c.CardName
                                    }).ToList() ?? new List<Core.Models.Survey.ImageCards>(),
                                    imageDescriptions = t.imageDescriptions ?? new List<ImageDescriptionUI>(),
                                    projectCards = t.tankImageCards?.Select(c => new ProjectCardMapping {
                                        ProjectId = projectId,
                                        CardId = c.CardNumber > 0 ? c.CardNumber : c.Id,
                                        Src = c.src,
                                        AdditionalDescription = c.AdditionalDescription,
                                        CurrentCondition = c.CurrentCondition,
                                        DescriptionId = c.DescriptionId
                                    }).ToList() ?? new ProjectCardMapping[0].ToList(), // Fixed potential null
                                    GradingLabelName = t.GradingLabelName ?? new List<string>(),
                                    Grading = t.Grading?.Select(g => new Core.Models.Survey.Grading {
                                        Name = g.Name,
                                        Value = g.Value,
                                        Checkbox = g.Checkbox?.Select(c => new Core.Models.Survey.CheckBox { Name = c.Name, Value = c.Value }).ToList() ?? new List<Core.Models.Survey.CheckBox>()
                                    }).ToList() ?? new List<Core.Models.Survey.Grading>()
                                };
                                newSec.subSections.Add(newSub);
                            }
                            report.template.Sections.Add(newSec);
                        }
                    }
                }

                if (!usedDynReportForTanks && report?.tankReportView?.Sections != null && report.tankReportView.Sections.Count > 0)
                {
                    var tankSections = report.tankReportView.Sections
                        .GroupBy(t => t.TankName ?? "");

                    foreach (var group in tankSections)
                    {
                        var newSec = new Sections { 
                            SectionName = group.Key, 
                            subSections = new List<SubSection>() 
                        };
                        
                        foreach (var t in group)
                        {
                            var newSub = new SubSection {
                                Id = t.Id,
                                SubSectionName = t.Subheader ?? t.TankName,
                                SectionNotes = t.SectionNotes,
                                ImageCards = t.tankImageCards?.Select(c => new Core.Models.Survey.ImageCards {
                                    Id = c.CardNumber > 0 ? c.CardNumber : c.Id,
                                    FileName = c.CardName
                                }).ToList() ?? new List<Core.Models.Survey.ImageCards>(),
                                imageDescriptions = t.imageDescriptions ?? new List<ImageDescriptionUI>(),
                                projectCards = t.tankImageCards?.Select(c => new ProjectCardMapping {
                                    ProjectId = projectId,
                                    CardId = c.CardNumber > 0 ? c.CardNumber : c.Id,
                                    Src = c.src,
                                    AdditionalDescription = c.AdditionalDescription,
                                    CurrentCondition = c.CurrentCondition,
                                    DescriptionId = c.DescriptionId
                                }).ToList() ?? new List<ProjectCardMapping>(),
                                GradingLabelName = t.GradingLabelName ?? new List<string>(),
                                Grading = t.Grading?.Select(g => new Core.Models.Survey.Grading {
                                    Name = g.Name,
                                    Value = g.Value,
                                    Checkbox = g.Checkbox?.Select(c => new Core.Models.Survey.CheckBox { Name = c.Name, Value = c.Value }).ToList() ?? new List<Core.Models.Survey.CheckBox>()
                                }).ToList() ?? new List<Core.Models.Survey.Grading>()
                            };
                            newSec.subSections.Add(newSub);
                        }
                        report.template.Sections.Add(newSec);
                    }
                }

                var finalDoc = new Aspose.Pdf.Document();

                foreach (var section in report.template.Sections ?? new List<Sections>())
                {
                    foreach (var sub in section.subSections ?? new List<SubSection>())
                    {
                        var html = BuildPartSectionHtml(section.SectionName, sub);
                        var partDoc = GenerateFromHtml(html);
                        finalDoc.Pages.Add(partDoc.Pages);
                    }
                }

                using var stream = new MemoryStream();
                finalDoc.Save(stream);

                var vesselName = report?.Vessel?.VesselName ?? "Part_Report";
                return (stream.ToArray(), vesselName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Part PDF error: {ProjectId}", projectId);
                return (Array.Empty<byte>(), string.Empty);
            }
        }

        private string BuildPartSectionHtml(string sectionName, SubSection sub)
        {
            var sb = new StringBuilder();

            var displayTitle = sectionName;
            if (!string.IsNullOrEmpty(sub.SubSectionName) && !sectionName.EndsWith(sub.SubSectionName, StringComparison.OrdinalIgnoreCase))
            {
                displayTitle = $"{sectionName} — {sub.SubSectionName}";
            }

            sb.Append($@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'/>
<style>
  body {{ font-family: Arial, sans-serif; font-size: 11px; padding: 20px; color: #000; }}
  .section-title {{ font-size: 13px; font-weight: bold; border-bottom: 2px solid #000; 
                    padding-bottom: 5px; margin-bottom: 15px; }}
  .sub-title {{ font-size: 12px; font-weight: bold; margin: 10px 0 8px 0; color: #333; }}
  table {{ width: 100%; border-collapse: collapse; margin-bottom: 15px; }}
  td, th {{ border: 1px solid #ccc; padding: 6px 8px; font-size: 11px; }}
  th {{ background: #f0f0f0; font-weight: bold; }}
  .checked {{ text-align: center; font-weight: bold; color: #000; }}
  .notes {{ background: #fafafa; border: 1px solid #ddd; padding: 8px; 
            font-size: 11px; margin-top: 10px; min-height: 30px; }}

  .img-row {{
    display: flex;
    gap: 15px;
    margin-bottom: 20px;
    page-break-inside: avoid;
    align-items: flex-start;
  }}
  .img-cell {{
    flex: 0 0 calc(50% - 8px);
    max-width: calc(50% - 8px);
    text-align: center;
    page-break-inside: avoid;
    display: flex;
    flex-direction: column;
    border: 1px solid #ccc;
  }}
  .img-info-table {{
    width: 100%;
    border-collapse: collapse;
    border-bottom: 1px solid #ccc;
    flex: 0 0 auto;
    table-layout: fixed;
  }}
  .img-info-table td {{
    padding: 4px 6px;
    font-size: 10px;
    border: none;
    border-bottom: 1px solid #eee;
    vertical-align: top;
    min-height: 22px;
    height: 22px;
    word-wrap: break-word;
  }}
  .info-label {{ width: 40%; color: #555; font-weight: bold; }}
  .info-value {{ width: 60%; color: #000; }}
  .additional {{ color: #777; font-size: 9px; display: block; min-height: 13px; }}
  .img-cell img {{
    width: 100%;
    height: 180px;
    object-fit: cover;
    display: block;
  }}
</style>
</head>
<body>
<div class='section-title'>{displayTitle}</div>
");

            // Grading table
            if (sub.Grading?.Any() == true)
            {
                sb.Append("<div class='sub-title'>Select Grades</div>");
                sb.Append("<table><thead><tr>");
                sb.Append("<th style='width:35%'> </th>");
                sb.Append("<th style='width:15%'>Grading</th>");

                foreach (var label in sub.GradingLabelName ?? new List<string>())
                    sb.Append($"<th>{label}</th>");

                sb.Append("</tr></thead><tbody>");

                foreach (var grade in sub.Grading)
                {
                    sb.Append($"<tr><td>{grade.Name}</td>");
                    sb.Append($"<td style='text-align:center'>{grade.Value ?? ""}</td>");

                    foreach (var cb in grade.Checkbox ?? new List<CheckBox>())
                    {
                        var tick = cb.Value ? "✓" : "";
                        sb.Append($"<td class='checked'>{tick}</td>");
                    }

                    sb.Append("</tr>");
                }

                sb.Append("</tbody></table>");
            }

            // Notes
            sb.Append("<div class='sub-title'>Notes</div>");
            if (!string.IsNullOrEmpty(sub.SectionNotes))
            {
                sb.Append($"<div class='notes'>{sub.SectionNotes}</div>");
            }
            else
            {
                sb.Append("<div class='notes'>-</div>");
            }

            // Images — 2 per row
            var images = sub.projectCards?
                .Where(p => !string.IsNullOrEmpty(p.Src))
                .ToList() ?? new List<ProjectCardMapping>();

            if (images.Any())
            {
                sb.Append("<div style='page-break-before: always;'></div>");
                sb.Append("<div class='sub-title'>Tank Images</div>");
                sb.Append("<table style='width: 100%; table-layout: fixed; border-collapse: separate; border-spacing: 15px; margin-bottom: 20px;'>");

                int imagesOnPage = 0;
                for (int i = 0; i < images.Count; i += 2)
                {
                    if (imagesOnPage > 0 && imagesOnPage % 4 == 0)
                    {
                        sb.Append("</table>");
                        sb.Append("<div style='page-break-before: always;'></div>");
                        sb.Append("<div class='sub-title' style='margin-top:20px;'>Tank Images (Continued)</div>");
                        sb.Append("<table style='width: 100%; table-layout: fixed; border-collapse: separate; border-spacing: 15px; margin-bottom: 20px;'>");
                    }

                    sb.Append("<tr>");

                    sb.Append("<td style='width: 50%; vertical-align: top; border: 1px solid #ccc; background: #fff; padding: 0;'>");
                    RenderImageCell(sb, images[i], sub);
                    sb.Append("</td>");
                    imagesOnPage++;

                    sb.Append("<td style='width: 50%; vertical-align: top; border: 1px solid #ccc; background: #fff; padding: 0;'>");
                    if (i + 1 < images.Count)
                    {
                        RenderImageCell(sb, images[i + 1], sub);
                        imagesOnPage++;
                    }
                    else
                    {
                        sb.Append("<div style='border: none; background: none;'></div>");
                    }
                    sb.Append("</td>");

                    sb.Append("</tr>");
                }
                sb.Append("</table>");
            }

            sb.Append("</body></html>");
            return sb.ToString();
        }
        private void RenderImageCell(StringBuilder sb, ProjectCardMapping img, SubSection sub)
        {
            var card = sub.ImageCards?
                .FirstOrDefault(c => c.Id == img.CardId);

            var descText = sub.imageDescriptions?
                .FirstOrDefault(d => d.Id == img.DescriptionId)
                ?.Description ?? "";

            var additionalDesc = img.AdditionalDescription ?? "";

            var conditionText = img.CurrentCondition > 0
                ? "Selected Condition"
                : "Current Condition";

            sb.Append(@"<table class='img-info-table'>
        <tr>
            <td class='info-label'>Description</td>
            <td class='info-value'>");
            sb.Append(!string.IsNullOrEmpty(descText) ? descText : "-");

            sb.Append($"<span class='additional'>{(!string.IsNullOrEmpty(additionalDesc) ? additionalDesc : "&nbsp;")}</span>");

            sb.Append(@"</td>
        </tr>
        <tr>
            <td class='info-label'>File name</td>
            <td class='info-value'>");
            sb.Append(card?.FileName ?? "-");
            sb.Append(@"</td>
        </tr>
        <tr>
            <td class='info-label'>Condition</td>
            <td class='info-value'>");
            sb.Append(conditionText);
            sb.Append(@"</td>
        </tr>
    </table>");

            var imgSrc = img.Src ?? "";
            if (!imgSrc.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(imgSrc))
            {
                imgSrc = "data:image/jpeg;base64," + imgSrc;
            }

            sb.Append($"<div style='text-align:center; padding:10px;'><img src='{imgSrc}' style='width:100%; max-height:220px; display:inline-block;' /></div>");
        }
        public async Task<(byte[], string)> GenerateHIReportPdf(int projectId, int templateId)
        {
            try
            {
                SetLicense();
                var report = await _surveyReportService
                    .GetReport(projectId, templateId, new List<string>());

                var hi = report?.template?.handireport;
                if (hi == null)
                    return (Array.Empty<byte>(), string.Empty);

                var html = BuildHIReportHtml(hi, report);
                var doc = GenerateFromHtml(html);

                using var stream = new MemoryStream();
                doc.Save(stream);

                return (stream.ToArray(), "HI_Report");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HI PDF error");
                return (Array.Empty<byte>(), string.Empty);
            }
        }

        private string BuildHIReportHtml(HandIReport hi, Report report)
        {
            var html = ReadEmbeddedResource("HIReport.html");

            // ── Grading Table ─────────────────────────

            var gradingTable = string.Empty;

            if (hi.Grading?.Any() == true)
            {
                var sb = new StringBuilder();

                sb.Append("<table><thead><tr>");
                sb.Append("<th> </th>");
                sb.Append("<th>Grading</th>");

                foreach (var label in hi.GradingLabelName ?? new List<string>())
                {
                    sb.Append($"<th>{label}</th>");
                }

                sb.Append("</tr></thead><tbody>");

                foreach (var grade in hi.Grading)
                {
                    sb.Append("<tr>");

                    sb.Append($"<td>{grade.Name}</td>");

                    sb.Append(
                        $"<td style='text-align:center'>{grade.Value ?? ""}</td>");

                    foreach (var cb in grade.Checkbox ?? new List<CheckBox>())
                    {
                        sb.Append(
                            $"<td style='text-align:center'>" +
                            $"{(cb.Value ? "✓" : "")}" +
                            $"</td>");
                    }

                    sb.Append("</tr>");
                }

                sb.Append("</tbody></table>");

                gradingTable = sb.ToString();
            }

            return html

                .Replace("{{COMPANY_NAME}}",
                    hi.CompanyName ?? "-")

                .Replace("{{CERTIFICATE_NUMBER}}",
                    hi.CertificateNumber ?? "-")

                .Replace("{{EXPIRY_DATE}}",
                    hi.ExpiryDate != DateTime.MinValue
                        ? hi.ExpiryDate.ToString("dd-MMM-yyyy")
                        : "-")

                .Replace("{{ZONE_A}}",
                    hi.ThicknessMeasurementA ?? "-")

                .Replace("{{ZONE_B}}",
                    hi.ThicknessMeasurementB ?? "-")

                .Replace("{{ZONE_C}}",
                    hi.ThicknessMeasurementC ?? "-")

                .Replace("{{MAINTENANCE_EXECUTED}}",
                    Radio(hi.MaintenanceExecuted))

                .Replace("{{ADDITIONAL_COMMENTS}}",
                    hi.AdditionalSurveyorComments ?? "-")

                .Replace("{{ACCOMMODATION}}",
                    Radio(hi.Accommodation))

                .Replace("{{PHOTOGRAPHS}}",
                    Radio(hi.PhotoGraphs))

                .Replace("{{TEST_LOCATION}}",
                    hi.OperationalTestLocation ?? "-")

                .Replace("{{START_DATE}}",
                    hi.OperationalTestStart.ToString("dd-MMM-yyyy"))

                .Replace("{{FINISH_DATE}}",
                    hi.OperationalTestFinish.ToString("dd-MMM-yyyy"))

                .Replace("{{ASSOCIATED_REPORT_NUMBER}}",
                    hi.AssociatedReportNumber ?? "-")

                .Replace("{{ASSOCIATED_REPORT_DATE}}",
                    hi.AssociatedReportDate ?? "-")

                .Replace("{{SUBSTANTIAL_CORROSION}}",
                    Radio(hi.SubstantialCorrosionRemaining))

                .Replace("{{STATUTORY_DEFICIENCIES}}",
                    Radio(hi.StatutoryDeficiencies))

                .Replace("{{FOUND_SATISFACTORY}}",
                    Radio(hi.FoundSatisfactory))

                .Replace("{{UNDER_REMARKS}}",
                    Radio(hi.UnderRemarks))

                .Replace("{{SCOURING_OR_PITTING}}",
                    Radio(hi.ScouringOrPitting))

                .Replace("{{BOTTOM_PLATING}}",
                    Radio(hi.BottomPlattingScouringOrPitting))

                .Replace("{{REMARKS}}",
                    hi.Remarks ?? "-")

                .Replace("{{GRADING_TABLE}}",
                    gradingTable)

                .Replace("{{LEAD_SURVEYOR_NAME}}",
                    hi.LeadSurveyorName ?? "-")

                .Replace("{{LEAD_SURVEYOR_NUMBER}}",
                    hi.LeadSurveyorNumber ?? "-")

                .Replace("{{LAST_VISIT_DATE}}",
                    FormatReportDate(hi.LastVisitDate))

                .Replace("{{SURVEYOR_NAME}}",
                    hi.SurveyorName ?? "-")

                .Replace("{{SURVEYOR_NUMBER}}",
                    hi.SurveyorNumber ?? "-")

                .Replace("{{INDIVIDUAL_A}}",
                    hi.IndividualA ?? "-")

                .Replace("{{INDIVIDUAL_B}}",
                    hi.IndividualB ?? "-")

                .Replace("{{INDIVIDUAL_C}}",
                    hi.IndividualC ?? "-")

                .Replace("{{INDIVIDUAL_D}}",
                    hi.IndividualD ?? "-")

                .Replace("{{INDIVIDUAL_E}}",
                    hi.IndividualE ?? "-")

                .Replace("{{INDIVIDUAL_F}}",
                    hi.IndividualF ?? "-");
        }

        // Helper
        private string Radio(string val) => val switch
        {
            "Yes" => "<span class='radio-yes'>✓ Yes</span>",
            "No" => "<span class='radio-no'>✗ No</span>",
            "N/A" => "<span class='radio-na'>N/A</span>",
            _ => val ?? "-"
        };

        public async Task<(byte[], string)> GenerateFullReportPdf(int projectId, string currentUserName, int? templateSectionId = null, List<ExportPart> selectedSettings = null)
        {
            try
            {
                SetLicense();
                var finalDoc = new Aspose.Pdf.Document();

                var vmdReport = await _surveyReportService.GetReport(projectId, 0, new List<string>());

                if (vmdReport?.Vessel != null)
                {
                    AppendHtmlToDoc(finalDoc, BuildVesselMainDataHtml(vmdReport));
                    AppendHtmlToDoc(finalDoc, BuildSurveyStatusHtml(vmdReport));
                    AppendHtmlToDoc(finalDoc, BuildStatutoryHtml(vmdReport));
                    AppendHtmlToDoc(finalDoc, BuildEvaluationGradingHtml());
                }

                var indexResult = await _surveyReportService.GetIndexAsync(projectId, currentUserName, templateSectionId);

                var reportIndex = indexResult?.Data?.ReportPartGrid
                    ?? new List<ReportPart>();

                var partTemplates = reportIndex
                    .Where(x => x.Name.StartsWith("Part ")
                             && x.Name != "Part H & I")
                    .OrderBy(x => x.Name)
                    .ToList();

                foreach (var part in partTemplates)
                {
                    try
                    {
                        List<string> sectionIds = new List<string>();
                        if (selectedSettings != null && selectedSettings.Any())
                        {
                            var partSettings = selectedSettings.FirstOrDefault(p => (p.PartName ?? "").ToLower().Trim() == (part.Name ?? "").ToLower().Trim());
                            if (partSettings == null || !partSettings.IncludeInReport)
                            {
                                continue;
                            }
                            if (partSettings.sections != null)
                            {
                                sectionIds = partSettings.sections.Select(s => s.SectionId.ToString()).ToList();
                            }
                        }

                        _logger.LogInformation("Processing part: {PartName}, TemplateId: {TemplateId}",
                            part.Name, part.TemplateId);

                        var partReport = await _surveyReportService.GetReport(projectId, part.TemplateId, sectionIds);
                        bool usedDynReportForTanks = false;
                        if ((partReport?.template?.Sections == null || partReport.template.Sections.Count == 0) && (partReport?.tankReportView?.Sections == null || partReport.tankReportView.Sections.Count == 0) && part.TemplateId != 0)
                        {
                            usedDynReportForTanks = true;
                            if (partReport == null) partReport = new Core.Models.Survey.Report { template = new Core.Models.Survey.View() };
                            if (partReport.template == null) partReport.template = new Core.Models.Survey.View();
                            partReport.template.Sections = new List<Sections>();
                            
                            int fallbackImo = vmdReport?.Vessel?.ImoNumber ?? 0;
                            var dynReport = await _projectReportService.GetReport(projectId, part.TemplateId, fallbackImo, sectionIds);
                            
                            if (dynReport?.template?.Sections?.subSections != null)
                            {
                                var dynTemplate = dynReport.template;
                                var grouped = dynTemplate.Sections.subSections
                                    .Where(s => sectionIds.Count == 0 || sectionIds.Contains(s.SectionMappingId.ToString()))
                                    .GroupBy(s => s.SectionName ?? "");
                                
                                foreach(var group in grouped)
                                {
                                    var newSec = new Sections { 
                                        SectionName = group.Key, 
                                        subSections = new List<SubSection>() 
                                    };
                                    
                                    foreach(var sub in group)
                                    {
                                        var newSub = new SubSection {
                                            Id = sub.Id,
                                            SubSectionName = sub.SubHeader,
                                            SectionNotes = sub.SectionNotes,
                                            ImageCards = sub.ImageCards?.Select(c => new Core.Models.Survey.ImageCards {
                                                Id = c.CardNumber > 0 ? c.CardNumber : c.Id,
                                                FileName = c.CardName
                                            }).ToList() ?? new List<Core.Models.Survey.ImageCards>(),
                                            imageDescriptions = sub.imageDescriptions ?? new List<ImageDescriptionUI>(),
                                            projectCards = sub.projectCards ?? new List<ProjectCardMapping>(),
                                            GradingLabelName = sub.GradingLabelName ?? new List<string>(),
                                            Grading = sub.Grading?.Select(g => new Core.Models.Survey.Grading {
                                                Name = g.Name,
                                                Value = g.Value,
                                                Checkbox = g.Checkbox?.Select(c => new Core.Models.Survey.CheckBox { Name = c.Name, Value = c.Value }).ToList() ?? new List<Core.Models.Survey.CheckBox>()
                                            }).ToList() ?? new List<Core.Models.Survey.Grading>()
                                        };
                                        newSec.subSections.Add(newSub);
                                    }
                                    partReport.template.Sections.Add(newSec);
                                }
                            }

                            if (dynReport?.tankReportView?.Sections != null)
                            {
                                var tankSections = dynReport.tankReportView.Sections
                                    .GroupBy(t => t.TankName ?? "");

                                foreach (var group in tankSections)
                                {
                                    var newSec = new Sections { 
                                        SectionName = group.Key, 
                                        subSections = new List<SubSection>() 
                                    };
                                    
                                    foreach (var t in group)
                                    {
                                        var newSub = new SubSection {
                                            Id = t.Id,
                                            SubSectionName = t.Subheader ?? t.TankName,
                                            SectionNotes = t.SectionNotes,
                                            ImageCards = t.tankImageCards?.Select(c => new Core.Models.Survey.ImageCards {
                                                Id = c.CardNumber > 0 ? c.CardNumber : c.Id,
                                                FileName = c.CardName
                                            }).ToList() ?? new List<Core.Models.Survey.ImageCards>(),
                                            imageDescriptions = t.imageDescriptions ?? new List<ImageDescriptionUI>(),
                                            projectCards = t.tankImageCards?.Select(c => new ProjectCardMapping {
                                                ProjectId = projectId,
                                                CardId = c.CardNumber > 0 ? c.CardNumber : c.Id,
                                                Src = c.src,
                                                AdditionalDescription = c.AdditionalDescription,
                                                CurrentCondition = c.CurrentCondition,
                                                DescriptionId = c.DescriptionId
                                            }).ToList() ?? new ProjectCardMapping[0].ToList(),
                                            GradingLabelName = t.GradingLabelName ?? new List<string>(),
                                            Grading = t.Grading?.Select(g => new Core.Models.Survey.Grading {
                                                Name = g.Name,
                                                Value = g.Value,
                                                Checkbox = g.Checkbox?.Select(c => new Core.Models.Survey.CheckBox { Name = c.Name, Value = c.Value }).ToList() ?? new List<Core.Models.Survey.CheckBox>()
                                            }).ToList() ?? new List<Core.Models.Survey.Grading>()
                                        };
                                        newSec.subSections.Add(newSub);
                                    }
                                    partReport.template.Sections.Add(newSec);
                                }
                            }
                        }

                        if (!usedDynReportForTanks && partReport?.tankReportView?.Sections != null && partReport.tankReportView.Sections.Count > 0)
                        {
                            var tankSections = partReport.tankReportView.Sections
                                .GroupBy(t => t.TankName ?? "");

                            foreach (var group in tankSections)
                            {
                                var newSec = new Sections { 
                                    SectionName = group.Key, 
                                    subSections = new List<SubSection>() 
                                };
                                
                                foreach (var t in group)
                                {
                                    var newSub = new SubSection {
                                        Id = t.Id,
                                        SubSectionName = t.Subheader ?? t.TankName,
                                        SectionNotes = t.SectionNotes,
                                        ImageCards = t.tankImageCards?.Select(c => new Core.Models.Survey.ImageCards {
                                            Id = c.CardNumber > 0 ? c.CardNumber : c.Id,
                                            FileName = c.CardName
                                        }).ToList() ?? new List<Core.Models.Survey.ImageCards>(),
                                        imageDescriptions = t.imageDescriptions ?? new List<ImageDescriptionUI>(),
                                        projectCards = t.tankImageCards?.Select(c => new ProjectCardMapping {
                                            ProjectId = projectId,
                                            CardId = c.CardNumber > 0 ? c.CardNumber : c.Id,
                                            Src = c.src,
                                            AdditionalDescription = c.AdditionalDescription,
                                            CurrentCondition = c.CurrentCondition,
                                            DescriptionId = c.DescriptionId
                                        }).ToList() ?? new List<ProjectCardMapping>(),
                                        GradingLabelName = t.GradingLabelName ?? new List<string>(),
                                        Grading = t.Grading?.Select(g => new Core.Models.Survey.Grading {
                                            Name = g.Name,
                                            Value = g.Value,
                                            Checkbox = g.Checkbox?.Select(c => new Core.Models.Survey.CheckBox { Name = c.Name, Value = c.Value }).ToList() ?? new List<Core.Models.Survey.CheckBox>()
                                        }).ToList() ?? new List<Core.Models.Survey.Grading>()
                                    };
                                    newSec.subSections.Add(newSub);
                                }
                                if (partReport.template.Sections == null) partReport.template.Sections = new List<Sections>();
                                partReport.template.Sections.Add(newSec);
                            }
                        }

                        _logger.LogInformation("Part {PartName} sections count: {Count}",
                               part.Name,
                               partReport?.template?.Sections?.Count ?? 0);

                        foreach (var section in partReport?.template?.Sections
                            ?? new List<Sections>())
                        {
                            _logger.LogInformation("Section: {SectionName}, SubSections: {Count}",
                                section.SectionName,
                                section.subSections?.Count ?? 0);

                            foreach (var sub in section.subSections
                                ?? new List<SubSection>())
                            {
                                var html = BuildPartSectionHtml($"{part.Name} — {section.SectionName}", sub);
                                AppendHtmlToDoc(finalDoc, html);
                            }
                        }
                    }
                    catch (Exception partEx)
                    {
                        _logger.LogWarning(partEx, "Part {PartName} failed, skipping", part.Name);
                    }
                }

                var hiTemplate = reportIndex.FirstOrDefault(x => x.Name == "Part H & I");

                if (hiTemplate != null)
                {
                    try
                    {
                        var proceed = true;
                        if (selectedSettings != null && selectedSettings.Any())
                        {
                            var partSettings = selectedSettings.FirstOrDefault(p => (p.PartName ?? "").ToLower().Trim() == "part h & i");
                            if (partSettings == null || !partSettings.IncludeInReport)
                            {
                                proceed = false;
                            }
                        }

                        if (proceed)
                        {
                            var hiReport = await _surveyReportService.GetReport(projectId, hiTemplate.TemplateId, new List<string>());

                            var hi = hiReport?.template?.handireport;
                            if (hi != null)
                            {
                                AppendHtmlToDoc(finalDoc, BuildHIReportHtml(hi, hiReport));
                            }
                        }
                    }
                    catch (Exception hiEx)
                    {
                        _logger.LogWarning(hiEx, "HI part failed, skipping");
                    }
                }

                using var stream = new MemoryStream();
                finalDoc.Save(stream);

                return (stream.ToArray(), "Full_CAP_Report");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Full report PDF error: {ProjectId}", projectId);
                return (Array.Empty<byte>(), string.Empty);
            }
        }

        private void AppendHtmlToDoc(Aspose.Pdf.Document targetDoc, string htmlContent)
        {
            try
            {
                var htmlStream = new MemoryStream(
                    Encoding.UTF8.GetBytes(htmlContent));

                var options = new HtmlLoadOptions
                {
                    PageInfo = new PageInfo
                    {
                        Width = PageSize.A4.Width,
                        Height = PageSize.A4.Height,
                        Margin = new MarginInfo(40, 80, 40, 60)
                    },
                    IsEmbedFonts = true,
                    InputEncoding = "UTF-8"
                };

                var partDoc = new Aspose.Pdf.Document(
                    htmlStream, options);

                foreach (Aspose.Pdf.Page page in partDoc.Pages)
                {
                    targetDoc.Pages.Add(page);
                }

                htmlStream.Dispose();
                partDoc.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Section append failed, skipping");
            }
        }
        private string BuildEvaluationGradingHtml()
        {
            return ReadEmbeddedResource("VesselEvaluationGrading.html");
        }

    }
}



