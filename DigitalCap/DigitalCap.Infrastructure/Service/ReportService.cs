using DigitalCap.Core.Constants;
using DigitalCap.Core.Helpers;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Identity;
using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Service
{
    public class ReportService : IReportService
    {
        private readonly IProjectService _projectService;
        private readonly ITaskRepository _taskRepository;
        private readonly ISecurityService _securityService;
        private readonly IUserPermissionRepository _userPermissionRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProjectRepository _projectRepository;

        public ReportService(
            IProjectService projectService,
            ITaskRepository taskRepository,
            ISecurityService securityService,
            IUserPermissionRepository userPermissionRepository,
            UserManager<ApplicationUser> userManager,
            IProjectRepository projectRepository)
        {
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _userPermissionRepository = userPermissionRepository ?? throw new ArgumentNullException(nameof(userPermissionRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _projectRepository = projectRepository;
        }

        public async Task<byte[]> ExportAllProjectsAsExcelSpreadsheet()
        {
            var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("All Projects");
            var rowCount = 0;

            // Retrieve columns for the Excel file
            var columns = GetExcelColumns();

            // Build header row
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;

            var headerCellStyle = workbook.CreateCellStyle();
            headerCellStyle.SetFont(headerFont);

            var headerRow = sheet.CreateRow(rowCount++);
            columns
                .Select((column, index) =>
                {
                    var cell = headerRow.CreateCell(index);
                    cell.SetCellValue(column.Item1);
                    cell.CellStyle = headerCellStyle;
                    return cell;
                })
                .ToList();

            // Retrieve projects and user details
            var projects = new List<Project>();

            var currentUserResult = await _securityService.GetCurrentUserAsync();

            var identityUser = await _userManager.FindByNameAsync(currentUserResult.Data.UserName);

            var userRoles = (await _userManager.GetRolesAsync(identityUser)).ToList();

            var isAdmin = userRoles.Contains("Admin (CAP HQ)");
            var isContributor = userRoles.Contains("Contributor (CAP Coordinator)");

            if (!isAdmin && !isContributor)
            {
                projects = await _projectRepository.GetAllProjects(currentUserResult.Data.UserName);
            }
            else
            {
                projects = await _projectRepository.GetAllProjects(null);
            }
            // Build rows
            projects.ForEach(project =>
            {
                var row = sheet.CreateRow(rowCount++);
                columns.Select((column, index) =>
                {
                    var cell = row.CreateCell(index);
                    var cellValue = column.Item2(project)?.ToString();
                    cell.SetCellValue(cellValue);
                    return cell;
                }).ToList();
            });

            // Adjust column widths
            columns.Select((_, index) =>
            {
                sheet.AutoSizeColumn(index);
                var width = sheet.GetColumnWidth(index);
                sheet.SetColumnWidth(index, (int)(width * 1.3));
                return index;
            }).ToList();

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

        public async Task<byte[]> ExportAllProjectsGridAsExcelSpreadsheet()
        {
            var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("All Projects");
            var rowCount = 0;
            var columns = GetAllProjectsGridExcelColumns();

            // Build header
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            var headerCellStyle = workbook.CreateCellStyle();
            headerCellStyle.SetFont(headerFont);

            var headerRow = sheet.CreateRow(rowCount++);
            columns.Select((col, index) =>
            {
                var cell = headerRow.CreateCell(index);
                cell.SetCellValue(col.Item1);
                cell.CellStyle = headerCellStyle;
                return cell;
            }).ToList();

            // Get user details and projects
            var currentUserResult = await _securityService.GetCurrentUserAsync();
            if (!currentUserResult.IsSuccess || currentUserResult.Data == null)
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }

            var currentUserDto = currentUserResult.Data;
            var identityUser = await _userManager.FindByNameAsync(currentUserDto.UserName);
            if (identityUser == null)
            {
                throw new UnauthorizedAccessException("Identity user not found");
            }

            var userRoles = (await _userManager.GetRolesAsync(identityUser)).ToHashSet();
            var isAdmin = userRoles.Contains("Admin (CAP HQ)");
            var isContributor = userRoles.Contains("Contributor (CAP Coordinator)");

            // Get projects grid data
            var projectsResult = await _projectService.GetAllProjectsGridData(false, currentUserDto.UserName, refreshStalePercentComplete: false);
            if (!projectsResult.IsSuccess)
            {
                throw new Exception("Failed to retrieve projects");
            }

            var projects = projectsResult.Data;

            if (!isAdmin && !isContributor)
            {
                var userProjects = await _userPermissionRepository.GetCurrentUserProjects(currentUserDto.UserName);
                var userProjectsSet = userProjects.ToHashSet();
                projects = projects.Where(project => project.Id.HasValue && userProjectsSet.Contains(project.Id.Value)).ToList();
            }

            // Convert to AllProjectsGridExport - simplified conversion
            var taskCache = new TaskCache(_taskRepository);
            var projectsExport = projects.Select(p => ConvertToAllProjectsGridExport(p, taskCache)).ToList();

            // Build project rows
            projectsExport.Select(project =>
            {
                var row = sheet.CreateRow(rowCount++);
                columns.Select((col, index) =>
                {
                    var cell = row.CreateCell(index);
                    cell.SetCellValue(col.Item2(project)?.ToString());
                    return cell;
                }).ToList();
                return row;
            }).ToList();

            // Fit columns to content
            columns.Select((_, index) =>
            {
                sheet.AutoSizeColumn(index);
                var width = sheet.GetColumnWidth(index);
                sheet.SetColumnWidth(index, (int)(width * 1.3));
                return index;
            }).ToList();

            // Return workbook data
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

        public async Task<List<Dictionary<string, string>>> ExportProjectsPartial(int minRange, int maxRange)
        {
            try
            {
                var columns = GetAllProjectsGridExcelColumns();

                var currentUserResult = await _securityService.GetCurrentUserAsync();
                if (!currentUserResult.IsSuccess || currentUserResult.Data == null)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
                }

                var currentUserDto = currentUserResult.Data;
                var identityUser = await _userManager.FindByNameAsync(currentUserDto.UserName);
                if (identityUser == null)
                {
                    throw new UnauthorizedAccessException("Identity user not found");
                }

                var userRoles = (await _userManager.GetRolesAsync(identityUser)).ToHashSet();
                var isAdmin = userRoles.Contains("Admin (CAP HQ)");
                var isContributor = userRoles.Contains("Contributor (CAP Coordinator)");

                var projectsResult = await _projectService.GetAllProjectsGridExport();
                if (!projectsResult.IsSuccess)
                {
                    throw new Exception("Failed to retrieve projects");
                }

                var projects = projectsResult.Data;

                if (!isAdmin && !isContributor)
                {
                    var userProjects = await _userPermissionRepository.GetCurrentUserProjects(currentUserDto.UserName);
                    var userProjectsSet = userProjects.ToHashSet();
                    projects = projects
                        .Where(project => project.ID.HasValue && userProjectsSet.Contains(project.ID.Value))
                        .ToList();
                }

                List<Dictionary<string, string>> partialRecords = new List<Dictionary<string, string>>();

                var selectedProjects = projects.Where((value, index) => index >= minRange && index < maxRange).ToList();

                foreach (var project in selectedProjects)
                {
                    Dictionary<string, string> record = new Dictionary<string, string>();
                    foreach (var col in columns)
                    {
                        var result = col.Item2(project)?.ToString();
                        record.Add(col.Item1.ToString(), result == null ? "" : result);
                    }
                    partialRecords.Add(record);
                }

                return partialRecords;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<Dictionary<string, string>>> ExportAllProjectsForUserAsync(string userName)
        {
            var columns = GetAllProjectsGridExcelColumns();

            var identityUser = await _userManager.FindByNameAsync(userName);
            if (identityUser == null)
            {
                throw new UnauthorizedAccessException("Identity user not found");
            }

            var userRoles = (await _userManager.GetRolesAsync(identityUser)).ToHashSet();
            var isAdmin = userRoles.Contains("Admin (CAP HQ)");
            var isContributor = userRoles.Contains("Contributor (CAP Coordinator)");

            var projectsResult = await _projectService.GetAllProjectsGridExport();
            if (!projectsResult.IsSuccess)
            {
                throw new Exception("Failed to retrieve projects");
            }

            var projects = projectsResult.Data;

            if (!isAdmin && !isContributor)
            {
                var userProjects = await _userPermissionRepository.GetCurrentUserProjects(userName);
                var userProjectsSet = userProjects.ToHashSet();
                projects = projects
                    .Where(project => project.ID.HasValue && userProjectsSet.Contains(project.ID.Value))
                    .ToList();
            }

            List<Dictionary<string, string>> allRecords = new List<Dictionary<string, string>>();

            foreach (var project in projects)
            {
                Dictionary<string, string> record = new Dictionary<string, string>();
                foreach (var col in columns)
                {
                    var result = col.Item2(project)?.ToString();
                    record.Add(col.Item1.ToString(), result == null ? "" : result);
                }
                allRecords.Add(record);
            }

            return allRecords;
        }
        private (string, Func<Project, object>)[] GetExcelColumns()
        {
            var taskCache = new TaskCache(_taskRepository);
            var rowCounter = 1;

            return new (string, Func<Project, object>)[]
            {
                ("Item #", p => rowCounter++),
                ("Project Status ", p => p.ProjectStatusName ?? ""),
                ("Region", p => p.CapRegion?.ToString() ?? ""),
                ("IMO number", p => p.IMO),
                ("Vessel Name", p => p.VesselName),
                ("PID", p => p.PIDNumber),
                ("Owner Name", p => p.CompanyName),
                ("WCN", p => p.WCN),
                ("Class", p => p.AbsClassID),
                ("Year Built", p => p.YearBuilt),
                ("Consideration (I/R)", p => p.CapType.GetDescription()),
                ("Type of Ship", p => p.ShipType.ToString()),
                ("Agreement Date", p => p.AgreementDate?.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture)),
                ("Agreement Signed Date", p => p.AgreementSignedDate?.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture)),
                ("Agreement under ABS (AS) or ABS Group (ABSG)", p => p.AgreementOwner),
                ("Proposal Number", p => p.AgreementNumber),
                ("Remark", p => p.ProjectComments)
            };
        }

        private (string, Func<AllProjectsGridExport, object>)[] GetAllProjectsGridExcelColumns()
        {
            var taskCache = new TaskCache(_taskRepository);
            var rowCounter = 1;

            return new (string, Func<AllProjectsGridExport, object>)[]
            {
                ("Item #", p => rowCounter++),
                ("Project Status ", p => p.ProjectStatusName),
                ("Region", p => p.CapRegion?.ToString() ?? string.Empty),
                ("IMO number", p => p.IMO),
                ("Vessel Name", p => p.VesselName),
                ("PID", p => p.PIDNumber),
                ("Billing Co - Owner", p => p.CompanyName),
                ("Contract Client - Mgmt Company", p => p.BillToCompanyName),
                ("WCN", p => p.WCN),
                ("Class", p => p.AbsClassID),
                ("Class Society", p => p.ClassSociety),
                ("Year Built", p => p.YearBuilt),
                ("Consideration (I/R)", p => p.CapType.GetDescription()),
                ("Type of Ship", p => p.ShipType.ToString()),
                ("Builder", p => p.Builder),
                ("Hull No.", p => p.HullNumber),
                ("Agreement Date", p => p.AgreementDate?.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture)),
                ("Agreement Signed Date", p => p.AgreementSignedDate?.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture)),
                ("Proposal Number", p => p.AgreementNumber),
                ("Request Record Review Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.RecordReviewRequested, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Record Review Received Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.RecordReviewReceived, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Record Review Status", p => taskCache.GetTaskValue(p.ID, (int)TaskId.RecordReviewReceived, x => x.Status)),
                ("SED Form Sent", p => taskCache.GetTaskValue(p.ID, (int)TaskId.RequestSenttoSED, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("PFA Rec'd From SED", p => taskCache.GetTaskValue(p.ID, (int)TaskId.PFAReceivedfromSED, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("PFA Sent to CAP L. Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.PFASenttoCAPLead, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Gauge Rec'd from CAP L. Date (OR UTM Report)", p => taskCache.GetTaskValue(p.ID, (int)TaskId.GaugingsReceivedfromCAPLead, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Structural Eval Rec'd Date (OR HSA)", p => taskCache.GetTaskValue(p.ID, (int)TaskId.StructuralEvaluationAndEvalGradeReceived, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Eval Grade ", p => p.StructuralGrade?.GetDescription()),
                ("Fatigue Grade", p => p.FatigueGrade?.GetDescription()),
                ("Renewal Grade", p => p.RenewalGrade?.GetDescription()),
                ("Material Grade", p => p.MaterialGrade?.GetDescription()),
                ("Gauging", p => p.GaugingGrade?.GetDescription()),
                ("Hull Girder Strength", p => p.HullGirderStrength?.GetDescription()),
                ("Planned Drydock Time", p => p.DrydockStart?.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture)),
                ("CAP Lead", p => p.LeadSurveyor),
                ("Lead Surveyor Assigned Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.FindAvailableSurveyor, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("CAP Team", p => p.SecondSurveyor),
                ("First Visit Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SurveyStarted, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Last Visit Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SurveyCompleted, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Survey Time Period", p =>
                {
                    var surveyStartedDaysWorked = taskCache.GetTaskValue(p.ID, (int)TaskId.SurveyStarted, x => x.DaysWorked);
                    var surveyCompletedDaysWorked = taskCache.GetTaskValue(p.ID, (int)TaskId.SurveyCompleted, x => x.DaysWorked);

                    if (!surveyStartedDaysWorked.HasValue && !surveyCompletedDaysWorked.HasValue)
                        return null;

                    return Math.Max(surveyStartedDaysWorked ?? int.MinValue, surveyCompletedDaysWorked ?? int.MinValue);
                }),
                ("10 Days Past Last Visit", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SurveyCompleted, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Survey Days", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SurveyReceived, x => x.SurveyDays?.ToString())),
                ("Survey Status ", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SurveyStarted, x => x.Status)),
                ("Drydock Location", p => p.DrydockLocationName),
                ("Report Date", p => p.ClassReportDate?.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture)),
                ("Report Rec'd Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.ReportReceivedbyCAPHQ, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Report Turnaround", p =>
                {
                    var reportCommencedDaysWorked = taskCache.GetTaskValue(p.ID, (int)TaskId.ReportCommencedbySurveyor, x => x.DaysWorked);
                    var reportReceivedDaysWorked = taskCache.GetTaskValue(p.ID, (int)TaskId.ReportReceivedbyCAPHQ, x => x.DaysWorked);

                    if (!reportCommencedDaysWorked.HasValue && !reportReceivedDaysWorked.HasValue)
                        return null;

                    return Math.Max(reportCommencedDaysWorked ?? int.MinValue, reportReceivedDaysWorked ?? int.MinValue);
                }),
                ("Sent to Chief Surveyor/ CAPCOM", p => taskCache.GetTaskValue(p.ID, (int)TaskId.DraftReportSenttoCAPCOM, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Cert. Date", p => p.CAPCertificateIssuanceDate?.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture)),
                ("Certificate Grade", p => p.FinalGrade?.GetDescription()),
                ("Grade Received Date from CAPCOM", p => taskCache.GetTaskValue(p.ID, (int)TaskId.CAPHQFinalizingReport, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Final Consolidated Report Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.ConsolidatedReportReadyforDelivery, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Cost Increase Request (X)", p => taskCache.GetTaskValue(p.ID, (int)TaskId.FirstCostIncreaseNoticeSent, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Invoice 1 Date Issued", p => taskCache.GetTaskValue(p.ID, (int)TaskId.FirstInvoiceRequested, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Invoice 1 Amount", p => taskCache.GetTaskValue(p.ID, (int)TaskId.FirstInvoiceRequested, x => x.Value)),
                ("Invoice 2 Date Issued", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SecondInvoiceRequested, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Invoice 2 Amount", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SecondInvoiceRequested, x => x.Value)),
                ("Final Invoice (3rd) Issued", p => taskCache.GetTaskValue(p.ID, (int)TaskId.ThirdInvoiceRequested, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Final Invoice (3rd) Amount", p => taskCache.GetTaskValue(p.ID, (int)TaskId.ThirdInvoiceRequested, x => x.Value)),
                ("Record Review IC Invoice Received Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.RRReceived, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Record Review IC Invoice Amount", p => taskCache.GetTaskValue(p.ID, (int)TaskId.RRReceived, x => x.Value)),
                ("SED IC Invoice 1 Received Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SED1Received, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("SED IC Invoice 1 Amount", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SED1Received, x => x.Value)),
                ("SED IC Invoice 2 Received Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SED2Received, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("SED IC Invoice 2 Amount", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SED2Received, x => x.Value)),
                ("Survey IC Invoice Received Date", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SurveyReceived, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Survey IC Invoice Amount", p => taskCache.GetTaskValue(p.ID, (int)TaskId.SurveyReceived, x => x.Value)),
                ("PCF Date (Project Closeout Form)", p => taskCache.GetTaskValue(p.ID, (int)TaskId.PCFCompleted, x => x.StatusDate?.Date.ToString(DateFormats.DisplayDate, CultureInfo.InvariantCulture))),
                ("Project Elapsed", p =>
                {
                    if (!p.ID.HasValue)
                        return null;

                    var tasks = taskCache[p.ID.Value];
                    return tasks.Max(x => x.DaysWorked);
                }),
                ("Remark", p => p.MostRecentProjectComment)
            };
        }

        private AllProjectsGridExport ConvertToAllProjectsGridExport(AllProjectsGrid grid, TaskCache taskCache)
        {
            int? pidNumber = null;
            if (!string.IsNullOrEmpty(grid.PIDNumber) && int.TryParse(grid.PIDNumber, out int pid))
            {
                pidNumber = pid;
            }

            int? imo = null;
            if (!string.IsNullOrEmpty(grid.IMO) && int.TryParse(grid.IMO, out int imoValue))
            {
                imo = imoValue;
            }

            return new AllProjectsGridExport
            {
                ID = grid.Id,
                Name = grid.Name,
                PIDNumber = pidNumber,
                CapRegion = grid.CapRegion,
                IMO = imo,
                VesselName = grid.VesselName,
                CompanyName = grid.CompanyName,
                WCN = grid.WCN,
                ProjectStatusName = grid.ProjectStatusName ?? grid.StatusName,
                MostRecentProjectComment = grid.MostRecentComment
            };
        }
    }

    public class TaskCache
    {
        private readonly ITaskRepository _taskRepository;
        private readonly Dictionary<int, List<SingleTask>> _cache = new Dictionary<int, List<SingleTask>>();

        public TaskCache(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public List<SingleTask> this[int projectId]
        {
            get
            {
                if (!_cache.ContainsKey(projectId))
                {
                    var tasks = _taskRepository.GetTasks(projectId).Result;
                    _cache[projectId] = tasks.ToList();
                }
                return _cache[projectId];
            }
        }

        public T GetTaskValue<T>(int? projectId, int taskId, Func<SingleTask, T> selector)
        {
            if (!projectId.HasValue)
                return default(T);

            var tasks = this[projectId.Value];
            var task = tasks.FirstOrDefault(t => t.TaskId == taskId);
            return task != null ? selector(task) : default(T);
        }
    }
}
