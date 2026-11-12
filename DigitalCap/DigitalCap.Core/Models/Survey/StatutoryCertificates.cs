using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Survey
{
    public class Certificates
    {
        public StatutoryCertificates StatutoryCertificatesExpirationDates { get; set; }
        public StatutoryCertificates StatutoryCertificatesIssuedDates { get; set; }
    }
    public class StatutoryCertificates
    {
        public DateTime? LLLIDate { get; set; }
        public DateTime? SafetyConDate { get; set; }
        public DateTime? SafetyRadioDate { get; set; }
        public DateTime? SafetyEquipmentDate { get; set; }
        public DateTime? IOPPDate { get; set; }
        public DateTime? IAPDate { get; set; }
        public DateTime? IAFSDate { get; set; }
        public DateTime? ISMDate { get; set; }
        public DateTime? ISPSDate { get; set; }
        public DateTime? CargoGearReTestDate { get; set; }
        public DateTime? CargoGearAnnualCertificate { get; set; }
        public DateTime? IBWMDate { get; set; }
        public DateTime? ISPPSewageDate { get; set; }
        public DateTime? IEEDate { get; set; }
        public DateTime? MaritimeLabourDate { get; set; }
        public DateTime? GasCarrierFitness { get; set; }
    }



    public class ReportStatutoryCertificates
    {
        public int projectId { get; set; }
        public int templateSectionId { get; set; }
        public DateTime llliIssueDate { get; set; }
        public DateTime llliExpiryDate { get; set; }
        public DateTime safetyConIssueDate { get; set; }
        public DateTime safetyConExpiryDate { get; set; }
        public DateTime safetyRadioIssueDate { get; set; }
        public DateTime safetyRadioExpiryDate { get; set; }
        public DateTime safetyEquipmentIssueDate { get; set; }
        public DateTime safetyEquipmentExpiryDate { get; set; }
        public DateTime ioppIssueDate { get; set; }
        public DateTime ioppExpiryDate { get; set; }
        public DateTime iapIssueDate { get; set; }
        public DateTime iapExpiryDate { get; set; }
        public DateTime iafsIsuueDate { get; set; }
        public DateTime iafsExpiryDate { get; set; }
        public DateTime ismIssueDate { get; set; }
        public DateTime ismExpiryDate { get; set; }
        public DateTime isExpiryDate { get; set; }
        public DateTime ispsIssueDate { get; set; }
        public DateTime ispsExpiryDate { get; set; }
        public DateTime cargoGearReTestIssueDate { get; set; }
        public DateTime cargoGearReTestExpiryDate { get; set; }
        public DateTime cargoGearAnnualIssueCertificate { get; set; }
        public DateTime cargoGearAnnualExpiryCertificate { get; set; }
        public DateTime ibwmIssueDate { get; set; }
        public DateTime ibwmExpiryDate { get; set; }
        public DateTime isppSewageIssueDate { get; set; }
        public DateTime isppSewageExpiryDate { get; set; }
        public DateTime ieeIssueDate { get; set; }
        public DateTime ieeExpiryDate { get; set; }
        public DateTime maritimeLabourIssueDate { get; set; }
        public DateTime maritimeLabourExpiryDate { get; set; }

        public DateTime fitnessGasCertificateIssueDate { get; set; }
        public DateTime fitnessGasCertificateExpiryDate { get; set; }

    }
}
