using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Survey;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class ProjectLandingViewModel
    {
        public ProgressModel ProgressModel { get; set; }
        public Project projectDetails { get; set; }
        public TasksViewModel TasksViewModel { get; set; }
        public ProjectDetailsViewModel ProjectDetailsViewModel { get; set; }
        public ReportDetailsViewModel ReportDetailsViewModel { get; set; }
        public IEnumerable<ProjectFile> ProjectFiles { get; set; }
        public byte PercentComplete { get; set; }
        public decimal? InvoiceSum { get; set; }
        public bool CancelProject { get; set; }
        public bool DeleteProject { get; set; }
        public bool FileDetails { get; set; }

        public bool ViewProject { get; set; }
        public bool RevenueTracker { get; set; }
        public IsSynchedOnline SynchedOnline { get; set; }
        public bool Release { get; set; }
    }
}
