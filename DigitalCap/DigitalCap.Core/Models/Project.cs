using DigitalCap.Core.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class Project : EntityBase<int>
    {
        public string Name { get; set; }
        public int? PIDNumber { get; set; }

        public string CapRegion { get; set; }
        public string CapScope { get; set; }
        public string CapScopeOther { get; set; }

        public CapType CapType { get; set; }

        public int? DrydockLocation { get; set; }
        public DateTime? DrydockStart { get; set; }
        public DateTime? DrydockEnd { get; set; }
        public DateTime? PotentialDrydockDate { get; set; }

        public DateTime? SurveyFirstVisit { get; set; }
        public DateTime? SurveyLastVisit { get; set; }

        public int? ProjectStatus { get; set; }
        public int? ProjectPriority { get; set; }

        public bool SpecialHull { get; set; } = true;
        public bool Fatigue { get; set; } = true;

        public int? ExpenseMarkup { get; set; }

        public decimal? SurveyDayrateClient { get; set; }
        public decimal? SurveyDayrateAbs { get; set; }
        public decimal? SedRate1 { get; set; }
        public decimal? SedRate2 { get; set; }

        public decimal? CapContractValue { get; set; }
        public string AgreementNumber { get; set; }
        public int? ProposalRev { get; set; }
        public string AgreementOwner { get; set; }

        public DateTime? AgreementDate { get; set; }
        public DateTime? AgreementSignedDate { get; set; }

        public string CertificateGrade { get; set; }
        public string ProjectComments { get; set; }

        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyBillingEmail { get; set; }
        public string CompanyBillingSystemUrl { get; set; }

        public bool BillSameAsCapClient { get; set; } = true;

        public string BillToCompanyName { get; set; }
        public string BillToCompanyAddress { get; set; }
        public string BillToCompanyEmail { get; set; }
        public string BillToCompanyBillingUrl { get; set; }

        public string PocName { get; set; }
        public string PocEmail { get; set; }
        public string PocPhone { get; set; }

        public int? IMO { get; set; }
        public string VesselName { get; set; }

        public string VesselType { get; set; }
        public ShipType ShipType { get; set; }

        public Guid? ClientProfileId { get; set; }
        public Guid? BillToClientProfileId { get; set; }

        public string WCN { get; set; }
        public string BillToWCN { get; set; }

        public string[] ClassSociety { get; set; }
        public string ClassSocietyOther { get; set; }

        public string AbsClassID { get; set; }

        public bool SisterVessel { get; set; } = false;
        public string SisterVesselImoNumber { get; set; }
        public string SisterVesselName { get; set; }

        public bool? IsDefaultTank { get; set; } = null;
        public int? CopyingVesselID { get; set; }

        public string Builder { get; set; }
        public string HullNumber { get; set; }
        public int? MonthBuilt { get; set; }
        public int? YearBuilt { get; set; }

        public decimal? LengthOverall { get; set; }

        public string CRMOpportunityNumber { get; set; }

        public string ProjectStatusName { get; set; }
        public string? ProjectStatusColor { get; set; }
        public string DrydockLocationName { get; set; }

        public string LeadSurveyor { get; set; }
        public string LeadSurveyorEID { get; set; }

        public string[] SecondSurveyor { get; set; }
        public string SecondSurveyorEID { get; set; }

        public string ThirdSurveyor { get; set; }
        public string ThirdSurveyorEID { get; set; }

        public string FourthSurveyor { get; set; }
        public string FourthSurveyorEID { get; set; }

        public bool AdditionalSurveyors { get; set; } = false;

        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }

        public string StatusChangedBy { get; set; }
        public DateTimeOffset? StatusChangedDate { get; set; }

        public string LastModifiedBy { get; set; }
        public byte PercentComplete { get; set; }

        public string TankVesselType { get; set; }
        public int TankVesselIMO { get; set; }

        public bool IsReadOnlyEnabled { get; set; }
    }

    public class ProjectStats
    {
        public int Total { get; set; }

        public int Online { get; set; }
        public int Offline { get; set; }
        
        public Dictionary<string, int> Regions { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Statuses { get; set; } = new Dictionary<string, int>();
    }
}
