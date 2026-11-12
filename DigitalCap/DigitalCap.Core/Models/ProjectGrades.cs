using DigitalCap.Core.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class ProjectGrades
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }

        public int? CAPCertificateNumber { get; set; }

        public int? ClassReportNumber { get; set; }

        public int? StructuralReportNumber { get; set; }

        public DateTime? CAPCertificateIssuanceDate { get; set; }

        public DateTime? ClassReportDate { get; set; }

        public DateTime? StructuralReportDate { get; set; }

        public FinalGrades? FinalGrade { get; set; }

        public Grades? StructuralGrade { get; set; }

        public Grades? FatigueGrade { get; set; }

        public Grades? RenewalGrade { get; set; }

        public Grades? MaterialGrade { get; set; }

        public Grades? GaugingGrade { get; set; }
        public bool ReportEdit { get; set; }
        public Grades? HullGirderStrength { get; set; }
        public DateTime? CAP1CertificateIssuanceDate { get; set; }
        public string CAP1FinalGrade { get; set; }
    }
}
