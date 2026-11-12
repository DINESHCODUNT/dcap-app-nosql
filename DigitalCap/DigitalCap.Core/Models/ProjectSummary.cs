using DigitalCap.Core.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class ProjectSummary
    {
        public int? ID { get; set; }
        public string Name { get; set; }
        public int? ProjectId { get; set; }
        public int? IMO { get; set; }
        public int? PIDNumber { get; set; }
        public string VesselName { get; set; }
        public ShipType ShipType { get; set; }
        public string CapRegion { get; set; }
        public int? YearBuilt { get; set; }
        public string AgreementOwner { get; set; }
        public string AgreementNumber { get; set; }
        public string ProjectComments { get; set; }
        public bool AreTechnicalTasksComplete { get; set; }
        public bool AreInvoiceTasksComplete { get; set; }
        public double? TechnicalTasksCompletionPercentage { get; set; }
        public double? InvoiceTasksCompletionPercentage { get; set; }
        public int? ProjectPriority { get; set; }
        public int? TechnicalDaysWorked { get; set; }
        public int? InvoiceDaysWorked { get; set; }
        public int? Success { get; set; }
        public string ResponseText { get; set; }

        public static ProjectSummary ErrorResponse(string message)
        {
            ProjectSummary model = new ProjectSummary();
            model.Success = 0;
            model.ResponseText = message;

            return model;
        }
    }
}