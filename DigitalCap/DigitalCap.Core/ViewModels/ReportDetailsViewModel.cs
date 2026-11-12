using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class ReportDetailsViewModel
    {
        public int GradesId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        [Display(Name = "CAP Certificate No.")]
        public int? CAPCertificateNumber { get; set; }

        [Display(Name = "Class Report No.")]
        public int? ClassReportNumber { get; set; }

        [Display(Name = "Structural Report No.")]
        public int? StructuralReportNumber { get; set; }

        [Display(Name = "CAP Certificate Issuance Date")]
        public DateTime? CAPCertificateIssuanceDate { get; set; }

        [Display(Name = "Class Report Date")]
        public DateTime? ClassReportDate { get; set; }

        [Display(Name = "Structural Report Date")]
        public DateTime? StructuralReportDate { get; set; }

        [Display(Name = "Final Grade")]
        public FinalGrades? FinalGrade { get; set; }

        [Display(Name = "Structural Grade")]
        public Grades? StructuralGrade { get; set; }

        [Display(Name = "Fatigue Grade")]
        public Grades? FatigueGrade { get; set; }

        [Display(Name = "Renewal Grade")]
        public Grades? RenewalGrade { get; set; }

        [Display(Name = "Material Grade")]
        public Grades? MaterialGrade { get; set; }

        [Display(Name = "Gauging Grade")]
        public Grades? GaugingGrade { get; set; }

        [Display(Name = "Hull Girder Strength")]
        public Grades? HullGirderStrength { get; set; }
        public DateTime? CAP1CertificateIssuanceDate { get; set; }
        public string CAP1FinalGrade { get; set; }
        public bool ReportDetailsComplete { get; set; }
        public bool ReportEdit { get; set; }
        public bool IsNewProject { get; set; }
        public int VesselTypeId { get; set; }
        public ReportDetailsViewModel()
        {

        }

        public ReportDetailsViewModel(ProjectGrades projectGrades)
        {
            GradesId = projectGrades.Id;
            ProjectId = projectGrades.ProjectId;
            CAPCertificateNumber = projectGrades.CAPCertificateNumber;
            ClassReportNumber = projectGrades.ClassReportNumber;
            StructuralReportNumber = projectGrades.StructuralReportNumber;
            CAPCertificateIssuanceDate = projectGrades.CAPCertificateIssuanceDate;
            ClassReportDate = projectGrades.ClassReportDate;
            StructuralReportDate = projectGrades.StructuralReportDate;
            FinalGrade = projectGrades.FinalGrade != null ? (FinalGrades)projectGrades.FinalGrade : null;
            StructuralGrade = projectGrades.StructuralGrade != null ? (Grades)projectGrades.StructuralGrade : null;
            FatigueGrade = projectGrades.FatigueGrade != null ? (Grades)projectGrades.FatigueGrade : null;
            RenewalGrade = projectGrades.RenewalGrade != null ? (Grades)projectGrades.RenewalGrade : null;
            MaterialGrade = projectGrades.MaterialGrade != null ? (Grades)projectGrades.MaterialGrade : null;
            GaugingGrade = projectGrades.GaugingGrade != null ? (Grades)projectGrades.GaugingGrade : null;
            HullGirderStrength = projectGrades.HullGirderStrength != null ? (Grades)projectGrades.HullGirderStrength : null;
            CAP1FinalGrade = projectGrades.CAP1FinalGrade;
            CAP1CertificateIssuanceDate = projectGrades.CAP1CertificateIssuanceDate;
            ReportEdit = false;
            ReportDetailsComplete = ReportRecordComplete(projectGrades);
        }

        private bool ReportRecordComplete(ProjectGrades project)
        {
            var nullList = new List<string>();
            foreach (PropertyInfo pi in project.GetType().GetProperties())
            {
                if (pi.GetValue(project) == null)
                {
                    nullList.Add(pi.Name);
                }
            }

            if (nullList.Count > 2)
            {
                return false;
            }
            else
            {
                foreach (var nullItem in nullList)
                {
                    switch (nullItem)
                    {
                        case nameof(FinalGrade):
                            if (nullList.Contains(nameof(CAP1FinalGrade)))
                            {
                                return false;
                            }
                            break;
                        case nameof(CAPCertificateIssuanceDate):
                            if (nullList.Contains(nameof(CAP1CertificateIssuanceDate)))
                            {
                                return false;
                            }
                            break;
                        case nameof(CAP1FinalGrade):
                            if (nullList.Contains(nameof(FinalGrade)))
                            {
                                return false;
                            }
                            break;
                        case nameof(CAP1CertificateIssuanceDate):
                            if (nullList.Contains(nameof(CAPCertificateIssuanceDate)))
                            {
                                return false;
                            }
                            break;
                        default:
                            return false;

                    }
                }
            }

            return true;
        }
    }

}
