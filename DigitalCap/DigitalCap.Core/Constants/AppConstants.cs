using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Constants
{
    public static class AppConstants
    {
        //App Globals
        public const string ABS_DIGITAL_PLATFORM = "ABS Digital Platform";
        //Roles and permissions
        public const string ORGANIZATION_ADMIN_FORMAT_STRING = "{0}_ADMINS";
        public const string ORGANIZATION_USERS_FORMAT_STRING = "{0}_USERS";
        public const string APPLICATION_USERS_FORMAT_STRING = "{0}_USERS";

        public const string ABS_Admins_Role = "ABS_Admins";
        public const string Client_Admin_Role = "Client Admin";
        public const string ABS_Company_Name = "ABS";

        public const string Admin_Cap_HQ = "Admin (CAP HQ)";
        public const string Contributor_CAP_Coordinator = "Contributor (CAP Coordinator)";

        //Application names
        public const string DigitalCAP = "DigitalCAP";
        public const string DCS = "DCS";
        public const string EmissionReporting = "EmissionReporting";
        public const string Gauging = "Gauging";
        public const string DataQuality = "DataQuality";
        public const string ConditionManager = "ConditionManager";
        public const string ClientAssetBaseline = "ClientAssetBaseline";
        public const string AnomalyDetection = "AnomalyDetection";
        public const string AISHindCast = "AISHindCast";
        public const string VesselInventory = "VesselInventory";
        public const string MRV = "MRV";
        public const string NLP = "NLP";
        public const string RemoteInspection = "RemoteInspection";
        public const string SuperMap = "SuperMap";
        public const string Corrosion = "CorrosionDetection";
        public const string CorrosionDetection = "CD";
        public const string Faq = "Faq";
        public const string Metocean = "Metocean";
        public const string StructuralDashboard = nameof(StructuralDashboard);
        //Request Statuses
        public const string OPEN = "OPEN";
        public const string PENDING = "PENDING";
        public const string APPROVED = "APPROVED";
        public const string DENIED = "DENIED";
        public const string DEFERRED = "DEFERRED";
        //Request Types
        public const string ORGANIZATION_MEMBERSHIP_REQUEST = "Join Organization";
        //stored procedure names
        public const string ADDEDIT_USER_WCN_MAPPING = "core.sp_ADDEDIT_USER_WCN_MAPPING";
        public const string DELETE_USER_WCN_MAPPING = "core.sp_DELETE_USER_WCN_MAPPING";
        public const string GetWCNGrid = "core.sp_GET_WCN_GRID";
        public const string PerformInitialLogin = "er.spAutoAssignCustomer";
        public static readonly string AddEditTermsOfUse = "core.sp_ADDEDIT_TERMS_OF_USE";
        public static readonly string GetTermsOfUseText = "core.sp_GET_TERMS_OF_USE_TEXT";
        public static readonly string GetTermsOfUse = "core.sp_GET_TERMS_OF_USE";
        public static readonly string AddNewWCN = "core.sp_ADD_NEW_WCN";

        // Configuration
        public const string VisibleTileConfigurationSectionName = "VisibleTiles";

        public static class Errors
        {
            public static class Views
            {
                public static readonly string GeneralErrorFieldName = "General";
            }
        }
        public static class Roles
        {
            public const string ABSAdmin = "ABS Admin";
            public const string ABSEngineers = "ABS Engineers";
            public const string ABSSurveyor = "ABS Surveyor";
            public const string Client = "Client";
            public const string ClientAuthorizedThirdParties = "Client Authorized 3rd Parties";
            public const string ClientAdmin = "Client Admin";
        }
    }
    /// <summary>
    /// The column names in the database
    /// </summary>
    public static class ColumnNames
    {
        public static string Vessel_ID = "Vessel_ID";
        public static string Source = "Source";
        public static string Beam = "Beam";
        public static string BillingCustomerCountry = "BillingCustomerCountry";
        public static string BillingCustomerID = "BillingCustomerID";
        public static string BillingCustomerName = "BillingCustomerName";
        public static string Breadth = "Breadth";
        public static string ContractDate = "ContractDate";
        public static string CustomerRole = "CustomerRole";
        public static string Depth = "Depth";
        public static string DesignTEU = "DesignTEU";
        public static string Draft = "Draft";
        public static string HullNum = "HullNum";
        public static string IMONum = "IMONum";
        public static string Length = "Length";
        public static string LimitDescription = "LimitDescription";
        public static string MarketSegment = "MarketSegment";
        public static string MoldedLength = "MoldedLength";
        public static string ProjectName = "ProjectName";
        public static string SteelCuttingDate = "SteelCuttingDate";
        public static string TEU = "TEU";
        public static string TEUDescription = "TEUDescription";
        public static string VesselName = "VesselName";
        public static string MarketSubSegment = "MarketSubSegment";
        public static string CallSign = "CallSign";
        public static string NetTonnage = "NetTonnage";
        public static string SisterGroup = "SisterGroup";
        public static string SisterType = "SisterType";
        public static string CommercialType = "CommercialType";
        public static string PRIMARY_HULL_MATERIAL = "PRIMARY_HULL_MATERIAL";
        public static string ShipTypeGrouping = "ShipTypeGrouping";
        public static string Statcode5 = "Statcode5";
        public static string Liquid = "Liquid";
        public static string Registered_Owner = "Registered_Owner";
        public static string Manager = "Manager";
        public static string Operator = "Operator";
        public static string Technical_Manager = "Technical_Manager";
        public static string Registered_Owner_ID = "Registered_Owner_ID";
        public static string Manager_ID = "Manager_ID";
        public static string Operator_ID = "Operator_ID";
        public static string Technical_Manager_ID = "Technical_Manager_ID";
        public static string BuildDate = "BuildDate";
        public static string Builder = "Builder";
        public static string BuilderName = "BuilderName";
        public static string BuilderNum = "BuilderNum";
        public static string ClassNum = "ClassNum";
        public static string ClassedDate = "ClassedDate";
        public static string Country = "Country";
        public static string DeliveryDate = "DeliveryDate";
        public static string FlagName = "FlagName";
        public static string KeelLayingDate = "KeelLayingDate";
        public static string LaunchingDate = "LaunchingDate";
        public static string OwnerCountry = "OwnerCountry";
        public static string OwnerID = "OwnerID";
        public static string OwnerName = "OwnerName";
        public static string VesselDescription = "VesselDescription";
        public static string VesselType = "VesselType";
        public static string CommercialCategory = "CommercialCategory";
        public static string ClassedBy = "ClassedBy";
        public static string ClassedBy2 = "ClassedBy2";
        public static string SubStatus = "SubStatus";
        public static string DWT = "DWT";
        public static string GT = "GT";

        //AD.AnomalySensorDetails columns
        public static string AnomalySensorDetailID = "AnomalySensorDetailID";
        public static string AlarmID = "AlarmID";
        public static string SensorID = "SensorID";
        public static string SensorValue = "SensorValue";
        public static string LastModifiedDate = "LastModifiedDate";
        public static string LastModifiedBy = "LastModifiedBy";
    }

    public static class VesselColumnNames
    {
        public static string DeliveryDate = "Delivery_Date";
        public static string BuilderName = "Builder_Name";
        public static string ClassedBy = "Classed_By";
        public static string ClassedBy2 = "Classed_By2";
        public static string ClassNum = "ClassNum";
        public static string HullNum = "Hull_Number";
        public static string Length = "Length";
        public static string VesselName = "Vessel_Name";
        public static string VesselType = "Vessel_Type";
    }

    public class ProjectPermissions
    {
        public bool AddProject { get; set; } = false;
        public bool ExportProject { get; set; } = false;
        public bool CancelProject { get; set; } = false;
        public bool DeleteProject { get; set; } = false;
    }



}
