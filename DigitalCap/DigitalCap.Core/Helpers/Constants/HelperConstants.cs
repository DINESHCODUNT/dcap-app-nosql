using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Helpers.Constants
{
    public static class HelperConstants
    {
        public static readonly int ER_AppId = 10;
        public static readonly int All_AppId = 20;
        public static readonly int CD_AppId = 30;
        public static readonly int DC_AppId = 40;

        public static readonly int VesselTypeDictionaryId = 1;

        public static string DateFormat = "dd-MMM-yyyy";
        public static string KendoDateFormat = $"{{0:{DateFormat}}}";
        public static string DateFormatString = "dd MMM yyyy";
        public static string DateTimeFormatString = "dd MMM yyyy hh:mm";
        public static string UNASSIGNED_CRITICAL_AREAS = "Unassigned Critical Areas";

        //stored procedure names

        public static readonly string GetDropdownItems = "core.sp_GET_DROPDOWN_ITEMS";
        public static readonly string GetDictionariesByIds = "core.sp_GET_DICTIONARIES_BY_IDS";
        public static readonly string GetEnrolledVessel = "er.sp_GET_ENROLLED_VESSEL";
        public static readonly string GetEnrolledVesselById = "er.sp_GET_ENROLLED_VESSEL_BY_ID";
        public static readonly string GetEnrolledVesselDetail = "er.sp_GET_ENROLLED_VESSEL_DETAIL";
        public static readonly string AddEditVessel = "er.sp_ADDEDIT_VESSEL";
        public static readonly string AddEditVesselV2 = "er.sp_ADDEDIT_VESSELV2";
        public static readonly string GetAllAuthorizedVessels = "er.sp_GET_ALL_AUTHORIZED_VESSELS";
        public static readonly string GetReportYearGrid = "er.sp_GET_REPORT_YEAR_GRID";
        public static readonly string GetReportYearDetailGrid = "er.sp_GET_REPORT_YEAR_DETAIL_GRID";
        public static readonly string GetVesselDetailsById = "er.sp_GET_VESSEL_DETAIL";
        public static readonly string SearchVesselIMO = "core.sp_SEARCH_VESSEL_IMO";
        public static readonly string GetAllPorts = "core.sp_GET_ALL_PORTS_AS_DV";
        public static readonly string GetReportedActivitySetup = "er.sp_GET_REPORTED_ACTIVITY_SETUP";
        public static readonly string GetReportedActivitySetupById = "er.sp_GET_REPORTED_ACTIVITY_SETUP_BY_ID";
        public static readonly string AddEditReportedActivitySetup = "er.sp_ADDEDIT_REPORTED_ACTIVITY_SETUP";
        public static readonly string GetReportedActivityVesselVoyages = "er.sp_GET_REPORTED_ACTIVITY_Voyages";
        public static readonly string GetReportedActivityVesselVoyagesById = "er.sp_GET_REPORTED_ACTIVITY_VOYAGES_BY_ID";
        public static readonly string AddEditReportedActivityVesselVoyages = "er.sp_ADDEDIT_REPORTED_ACTIVITY_Voyages";
        public static readonly string GetReportedActivityVesselPorts = "er.sp_GET_REPORTED_ACTIVITY_PORTS";
        public static readonly string GetReportedActivityVesselPortsById = "er.sp_GET_REPORTED_ACTIVITY_PORTS_BY_ID";
        public static readonly string AddEditReportedActivityVesselPorts = "er.sp_ADDEDIT_REPORTED_ACTIVITY_PORTS";
        public static readonly string DeleteAllReportDetailsByReportId = "er.sp_DELETE_REPORT_YEAR_VOYAGES_PORTS_BY_ID";
        public static readonly string GetPortsBySearchCriteria = "er.sp_GET_PORTS_BY_SEARCH_TEXT";
        public static readonly string GetAllPortModels = "er.sp_GET_ALL_PORTS";
        public static readonly string SubmitReportToABS = "er.sp_SUBMIT_REPORT";
        public static readonly string AddEditCalcData = "er.sp_ADDEDIT_CALC_DATA";
        public static readonly string GetNextCalcItem = "er.sp_GET_NEXT_CALC_ITEM";
        public static readonly string SubmitFinalizeReport = "er.sp_FINALIZE_REPORT";
        public static readonly string SubmitReportToVerifier = "er.sp_SUBMIT_REPORT_VERIFIER";
        public static readonly string GetReportYearSummary = "er.sp_GET_REPORT_YEAR_SUMMARY";
        public static readonly string GetReportYearSummaryById = "er.sp_GET_REPORT_YEAR_SUMMARY_BY_ID";
        public static readonly string SetIsProcessing = "er.sp_SET_IS_PROCESSING";
        public static readonly string SetNeedsProcessing = "er.sp_SET_NEEDS_PROCESSING";
        public static readonly string SetIsProcessingComplete = "er.sp_SET_PROCESSING_COMPLETE";
        public static readonly string AddEditAISPerformance = "er.sp_ADDEDIT_AIS_PERFORMANCE";
        public static readonly string LogDetailedAISData = "er.sp_LOG_DETAILED_AIS_DATA";
        public static readonly string AddEditEngineerClaimReportYear = "er.sp_ADDEDIT_CLAIM_REPORT_YEAR";
        public static readonly string AddEditTermsOfUse = "core.sp_ADDEDIT_TERMS_OF_USE";
        public static readonly string GetTermsOfUse = "core.sp_GET_TERMS_OF_USE";
        public static readonly string GetTermsOfUseText = "core.sp_GET_TERMS_OF_USE_TEXT";
        public static readonly string CheckPriorSubmissionForReportYear = "er.sp_CHECK_PRIOR_REPORT_ENTRIES";
        public static readonly string GetReporting = "er.sp_GET_REPORTING";
        public static readonly string GetReportingById = "er.sp_GET_REPORTING_BY_ID";
        public static readonly string SetSampleVoyage = "er.sp_ADD_EDIT_SAMPLE_VOY";
        public static readonly string AddDocument = "er.sp_INS_document";
        public static readonly string DeleteDocumentById = "er.SP_Del_DocumentById";
        public static readonly string GetDocumentById = "er.SP_Get_DocumentById";
        public static readonly string GetUploadedDocsByVesselYear = "er.SP_Get_UploadedDocumentsByVesselYear";
        public static readonly string GetUploadedDocsByReportYearId = "er.SP_Get_UploadedDocumentsByReportYearId";
        public static readonly string GetCorrectionRequestHeaders = "er.sp_GET_CorrectionRequestHeaders";
        public static readonly string GetCorrectionRequestHeader = "er.sp_GET_CorrectionRequestHeader";
        public static readonly string GetCorrectionRequest = "er.sp_GET_CorrectionRequest";
        public static readonly string AddEditCorrectionRequestHeader = "er.sp_ADDEDIT_CorrectionRequest_Header";
        public static readonly string AddEditCorrectionRequest = "er.sp_ADDEDIT_CorrectionRequest";
        public static readonly string DeleteMessageRequestHeader = "er.sp_DELETE_CorrectionRequest_Header";
        public static readonly string DeleteMessageRequest = "er.sp_DELETE_CorrectionRequest";
        public static readonly string GetNotifications = "er.sp_GET_NOTIFICATION";
        public static readonly string DeleteNotification = "er.sp_DELETE_NOTIFICATION";
        public static readonly string MarkNotificationRead = "er.sp_READ_NOTIFICATION";
        public static readonly string RejectReportVerification = "er.sp_REJECT_VERIFICATION";
        public static readonly string DeleteVessel = "er.sp_DELETE_VESSEL";
        public static readonly string SubmitSupportingDocs = "er.sp_SUBMIT_SUPPORTING_DOCS";
        public static readonly string GetAllVerifier = "er.sp_GET_ALL_VERIFIER";
        public static readonly string GetMyFleet = "er.sp_GET_MY_FLEET";
        public static readonly string GetReportGrid = "er.sp_GET_REPORT_GRID";
        public static readonly string GetBunkeringGrid = "er.sp_GET_BUNKERING_GRID";
        public static readonly string AddEditBunkeringNote = "er.sp_ADDEDIT_BUNKERING_NOTE";
        public static readonly string DeleteReportById = "er.sp_DELETE_REPORT_BY_ID";
        public static readonly string GetActivityDocs = "er.sp_GET_ACTIVITY_DOCS";
        public static readonly string ClientRequestSiteVisit = "er.sp_CLIENT_REQUEST_SITE_VISIT";
        public static readonly string SubmitSiteVisitor = "er.sp_SUBMIT_SITE_VISITOR";
        public static readonly string CancelSiteVisitRequest = "er.sp_CANCEL_SITE_VISIT";
        public static readonly string AddEditVesselReport = "er.sp_ADDEDIT_VESSEL_REPORT";
        public static readonly string GetVesselIdFromIMO = "er.sp_GET_VESSELID_FROM_IMO";
        public static readonly string GetContactUs = "er.sp_GET_CONTACTS";
        public static readonly string ResolveMessageRequestHeader = "er.sp_RESOLVE_CORRECTION_REQUEST";
        public static readonly string IMOWCNExists = "er.sp_IMO_WCN_EXISTS";
        public static readonly string GetCanEditReportYear = "er.sp_CAN_EDIT_REPORT_BY_ID";
        public static readonly string GetWCNsByUserName = "er.sp_GET_WCN_BY_USER";
        public static readonly string GetReportIsAuthorized = "er.sp_GET_REPORT_IS_AUTHORIZED";
        public static readonly string SelectSampleVoyages = "er.sp_SELECT_SAMPLE_VOYAGES";
        public static readonly string AddEditAISVoyageCalculation = "er.sp_ADDEDIT_AIS_VOYAGE_CALC";

        // Corrosion Detection
        public static readonly string GetCDDashboard = "cd.sp_GET_DASHBOARD";

        public static readonly string CreateCDProject = "cd.sp_CREATE_PROJECT";
        public static readonly string GetCDProject = "cd.sp_GET_PROJECT";

        public static readonly string CreateCDImageSet = "cd.sp_CREATE_IMAGE_SET";
        public static readonly string GetCDImageSets = "cd.sp_GET_IMAGE_SETS";
        public static readonly string GetCDImageSet = "cd.sp_GET_IMAGE_SET";
        public static readonly string UpdateCDImageSet = "cd.sp_UPDATE_IMAGE_SET";
        public static readonly string DeleteCDImageSet = "cd.sp_DELETE_IMAGE_SET";

        public static readonly string CreateCDImageSetNote = "cd.sp_CREATE_IMAGE_SET_NOTE";
        public static readonly string GetCDImageSetNote = "cd.sp_GET_IMAGE_SET_NOTES";

        //App Globals
        public const string ABS_DIGITAL_PLATFORM = "ABS Digital Platform";
        //Roles and permissions
        public const string ORGANIZATION_ADMIN_FORMAT_STRING = "{0}_ADMINS";
        public const string ORGANIZATION_USERS_FORMAT_STRING = "{0}_USERS";
        public const string APPLICATION_USERS_FORMAT_STRING = "{0}_USERS";

        public const string ABS_Admins_Role = "ABS_Admins";

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
        public static readonly string AddNewWCN = "core.sp_ADD_NEW_WCN";

        // Configuration
        public const string VisibleTileConfigurationSectionName = "VisibleTiles";

        public static object StoredProcedures { get; set; }
        public static object Roles { get; set; }
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

    public static class StoredProcedures
    {
        public static class UserAccounts
        {
            public static readonly string GetByAspNetId = "sp_UserAccounts_GetByAspNetId";
            public static readonly string GetByAspNetId_ActiveOrDeleted = "sp_UserAccounts_GetByAspNetId_ActiveOrDeleted";
            public static readonly string GetUsersInRole = "sp_UserAccounts_GetUsersInRole";
            public static readonly string GetUsersWithPermission = "sp_UserAccounts_GetUsersWithPermission";
            public static readonly string GetUsersInRoleForClient = "sp_UserAccounts_GetUsersInRoleForClient";
            public static readonly string GetAbsUsers = "sp_UserAccounts_GetAbsUsers";
            public static readonly string GetClientUsers = "sp_UserAccounts_GetClientUsers";
        }
    }

    public static class TableNames
    {
        public static string Vessel = "core.Vessel";
        public static string AnomalySensorDetails = "AD.AnomalySensorDetails";
    }

    public static class CategoryNames
    {
        public static string Invoicing = nameof(Invoicing);
        public static string Project = nameof(Project);
    }

    public static class TypeNames
    {
        public static string ClientInvoices = "Client Invoices";
    }

    public static class TaskNames
    {
        public static string Survey = nameof(Survey);
    }

    public static class PageNames
    {
        public static string Update = "All Details";
        public static string Invoicing = nameof(Invoicing);
        public static string Workflow = nameof(Workflow);
        public static string AdditionalFiles = "All File Uploads";
        public static string RecentActivity = "Recent Activity";
        public static string SurveyReport = "Report / Certificate";

    }

}
