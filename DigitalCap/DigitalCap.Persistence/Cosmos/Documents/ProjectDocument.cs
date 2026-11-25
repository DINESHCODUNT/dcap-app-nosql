using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ProjectDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("ID")]
        public int? ID { get; set; }

        [JsonProperty("Id")]
        public int? IdProp { get; set; }

        [JsonIgnore]
        public int EffectiveId => ID ?? IdProp ?? (int.TryParse(id, out var parsed) ? parsed : 0);

        [JsonProperty("PIDNumber")]
        public object? PIDNumber { get; set; }

        [JsonIgnore]
        public int? PIDNumberInt
        {
            get
            {
                if (PIDNumber == null) return null;
                if (PIDNumber is int i) return i;
                if (PIDNumber is long l) return (int)l;
                if (int.TryParse(PIDNumber.ToString(), out var parsed)) return parsed;
                return null;
            }
        }

        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("CapRegion")]
        public string? CapRegion { get; set; }

        [JsonProperty("CapScope")]
        public string? CapScope { get; set; }

        [JsonProperty("CapScopeOther")]
        public string? CapScopeOther { get; set; }

        [JsonProperty("CapType")]
        public int? CapType { get; set; }

        [JsonProperty("DrydockLocation")]
        public int? DrydockLocation { get; set; }

        [JsonProperty("DrydockStart")]
        public DateTime? DrydockStart { get; set; }

        [JsonProperty("DrydockEnd")]
        public DateTime? DrydockEnd { get; set; }

        [JsonProperty("PotentialDrydockDate")]
        public DateTime? PotentialDrydockDate { get; set; }

        [JsonProperty("SurveyFirstVisit")]
        public DateTime? SurveyFirstVisit { get; set; }

        [JsonProperty("SurveyLastVisit")]
        public DateTime? SurveyLastVisit { get; set; }

        [JsonProperty("ProjectPriority")]
        public int? ProjectPriority { get; set; }

        [JsonProperty("SpecialHull")]
        public bool? SpecialHull { get; set; }

        [JsonProperty("FatigueAnalysis")]
        public bool? FatigueAnalysis { get; set; }

        [JsonProperty("ExpenseMarkup")]
        public int? ExpenseMarkup { get; set; }

        [JsonProperty("SurveyDayRateClient")]
        public decimal? SurveyDayRateClient { get; set; }

        [JsonProperty("SurveyDayRateAbs")]
        public decimal? SurveyDayRateAbs { get; set; }

        [JsonProperty("SedRate1")]
        public decimal? SedRate1 { get; set; }

        [JsonProperty("SedRate2")]
        public decimal? SedRate2 { get; set; }

        [JsonProperty("CapContractValue")]
        public decimal? CapContractValue { get; set; }

        [JsonProperty("AgreementNumber")]
        public string? AgreementNumber { get; set; }

        [JsonProperty("ProposalRev")]
        public int? ProposalRev { get; set; }

        [JsonProperty("AgreementOwner")]
        public string? AgreementOwner { get; set; }

        [JsonProperty("AgreementDate")]
        public DateTime? AgreementDate { get; set; }

        [JsonProperty("AgreementSignedDate")]
        public DateTime? AgreementSignedDate { get; set; }

        [JsonProperty("CertificateGrade")]
        public string? CertificateGrade { get; set; }

        [JsonProperty("ProjectComments")]
        public string? ProjectComments { get; set; }

        [JsonProperty("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonProperty("CompanyAddress")]
        public string? CompanyAddress { get; set; }

        [JsonProperty("CompanyBillingEmail")]
        public string? CompanyBillingEmail { get; set; }

        [JsonProperty("CompanyBillingSystemUrl")]
        public string? CompanyBillingSystemUrl { get; set; }

        [JsonProperty("BillSameAsCapClient")]
        public bool? BillSameAsCapClient { get; set; }

        [JsonProperty("BillToCompanyName")]
        public string? BillToCompanyName { get; set; }

        [JsonProperty("BillToCompanyAddress")]
        public string? BillToCompanyAddress { get; set; }

        [JsonProperty("BillToCompanyEmail")]
        public string? BillToCompanyEmail { get; set; }

        [JsonProperty("BillToCompanyBillingUrl")]
        public string? BillToCompanyBillingUrl { get; set; }

        [JsonProperty("PocName")]
        public string? PocName { get; set; }

        [JsonProperty("PocEmail")]
        public string? PocEmail { get; set; }

        [JsonProperty("PocPhone")]
        public string? PocPhone { get; set; }

        [JsonProperty("IMO")]
        public int? IMO { get; set; }

        [JsonProperty("VesselName")]
        public string? VesselName { get; set; }

        [JsonProperty("VesselType")]
        public string? VesselType { get; set; }

        [JsonProperty("ShipType")]
        public object? ShipType { get; set; }

        [JsonProperty("ClientProfileId")]
        public Guid? ClientProfileId { get; set; }

        [JsonProperty("BillToClientProfileId")]
        public Guid? BillToClientProfileId { get; set; }

        [JsonProperty("WCN")]
        public string? WCN { get; set; }

        [JsonProperty("BillToWCN")]
        public string? BillToWCN { get; set; }

        [JsonProperty("ClassSociety")]
        public string? ClassSociety { get; set; }

        [JsonProperty("ClassSocietyOther")]
        public string? ClassSocietyOther { get; set; }

        [JsonProperty("AbsClassID")]
        public string? AbsClassID { get; set; }

        [JsonProperty("SisterVessel")]
        public bool? SisterVessel { get; set; }

        [JsonProperty("SisterVesselImoNumber")]
        public string? SisterVesselImoNumber { get; set; }

        [JsonProperty("SisterVesselName")]
        public string? SisterVesselName { get; set; }

        [JsonProperty("IsDefaultTank")]
        public bool? IsDefaultTank { get; set; }

        [JsonProperty("CopyingVesselID")]
        public int? CopyingVesselID { get; set; }

        [JsonProperty("Builder")]
        public string? Builder { get; set; }

        [JsonProperty("HullNumber")]
        public string? HullNumber { get; set; }

        [JsonProperty("MonthBuilt")]
        public int? MonthBuilt { get; set; }

        [JsonProperty("YearBuilt")]
        public int? YearBuilt { get; set; }

        [JsonProperty("LengthOverall")]
        public decimal? LengthOverall { get; set; }

        [JsonProperty("CRMOpportunityNumber")]
        public string? CRMOpportunityNumber { get; set; }

        [JsonProperty("LeadSurveyorEID")]
        public string? LeadSurveyorEID { get; set; }

        [JsonProperty("SecondSurveyorEID")]
        public string? SecondSurveyorEID { get; set; }

        [JsonProperty("ThirdSurveyor")]
        public string? ThirdSurveyor { get; set; }

        [JsonProperty("ThirdSurveyorEID")]
        public string? ThirdSurveyorEID { get; set; }

        [JsonProperty("FourthSurveyor")]
        public string? FourthSurveyor { get; set; }

        [JsonProperty("FourthSurveyorEID")]
        public string? FourthSurveyorEID { get; set; }

        [JsonProperty("AdditionalSurveyors")]
        public bool? AdditionalSurveyors { get; set; }

        [JsonProperty("CreatedBy")]
        public string? CreatedBy { get; set; }

        [JsonProperty("CreatedDate")]
        public DateTime? CreatedDate { get; set; }

        [JsonProperty("StatusChangedBy")]
        public string? StatusChangedBy { get; set; }

        [JsonProperty("StatusChangedDate")]
        public DateTimeOffset? StatusChangedDate { get; set; }

        [JsonProperty("ModifiedDate")]
        public DateTime? ModifiedDate { get; set; }

        [JsonProperty("LastModifiedBy")]
        public string? LastModifiedBy { get; set; }

        [JsonProperty("PercentComplete")]
        public byte? PercentComplete { get; set; }

        [JsonProperty("TankVesselType")]
        public string? TankVesselType { get; set; }

        [JsonProperty("TankVesselIMO")]
        public int? TankVesselIMO { get; set; }

        [JsonProperty("IsReadOnlyEnabled")]
        public bool? IsReadOnlyEnabled { get; set; }

        [JsonProperty("SurveyDays")]
        public int? SurveyDays { get; set; }

        [JsonProperty("SurveyDate")]
        public DateTime? SurveyDate { get; set; }

        [JsonProperty("CompanyId")]
        public int? CompanyId { get; set; }

        [JsonProperty("ClientId")]
        public int? ClientId { get; set; }

        [JsonProperty("IsActive")]
        public bool? IsActive { get; set; }

        [JsonProperty("ProjectStatus")]
        public int? ProjectStatus { get; set; }

        [JsonProperty("LeadSurveyor")]
        public string? LeadSurveyor { get; set; }

        [JsonProperty("SecondSurveyor")]
        public string? SecondSurveyor { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
