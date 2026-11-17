using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public static class CosmosContainers
    {
        #region SQL schema CAP (25 tables)
        public const string CAP_Activities = "CAP_Activities";
        public const string CAP_CAPCoordinators = "CAP_CAPCoordinators";
        public const string CAP_Classes = "CAP_Classes";
        public const string CAP_Clients = "CAP_Clients";
        public const string CAP_Comments = "CAP_Comments";
        public const string CAP_Comments_history = "CAP_Comments_history";
        public const string CAP_Comments_offline = "CAP_Comments_offline";
        public const string CAP_Files = "CAP_Files";
        public const string CAP_Lookups = "CAP_Lookups";
        public const string CAP_ProjectGrades = "CAP_ProjectGrades";
        public const string CAP_Projects = "CAP_Projects";
        public const string CAP_Projects_history = "CAP_Projects_history";
        public const string CAP_Projects_offline = "CAP_Projects_offline";
        public const string CAP_ProjectTaskStatus = "CAP_ProjectTaskStatus";
        public const string CAP_Reports = "CAP_Reports";
        public const string CAP_Statuses = "CAP_Statuses";
        public const string CAP_SurveyImages = "CAP_SurveyImages";
        public const string CAP_TaskDueDates = "CAP_TaskDueDates";
        public const string CAP_Tasks = "CAP_Tasks";
        public const string CAP_TaskStatusGroups = "CAP_TaskStatusGroups";
        public const string CAP_TemplateGradingSectionPhotoDescriptions = "CAP_TemplateGradingSectionPhotoDescriptions";
        public const string CAP_TemplateGradingSections = "CAP_TemplateGradingSections";
        public const string CAP_TemplateGradingSectionSurveyItems = "CAP_TemplateGradingSectionSurveyItems";
        public const string CAP_Templates = "CAP_Templates";
        public const string CAP_TemplateSections = "CAP_TemplateSections";
        #endregion

        #region SQL schema Config (16 tables)
        public const string Config_H_I_Grading = "Config_H_I_Grading";
        public const string Config_ProjectSectionDescriptionMapping = "Config_ProjectSectionDescriptionMapping";
        public const string Config_ProjectSectionGradingMapping = "Config_ProjectSectionGradingMapping";
        public const string Config_ProjectSectionMapping = "Config_ProjectSectionMapping";
        public const string Config_ProjectSubSection = "Config_ProjectSubSection";
        public const string Config_ProjectSubSectionDescriptionMapping = "Config_ProjectSubSectionDescriptionMapping";
        public const string Config_ProjectTankSectionMapping = "Config_ProjectTankSectionMapping";
        public const string Config_ReportPart = "Config_ReportPart";
        public const string Config_VesselTypeNormalSectionMapping = "Config_VesselTypeNormalSectionMapping";
        public const string Config_VesselTypePartMapping = "Config_VesselTypePartMapping";
        public const string Config_VesselTypeSectionDescriptionMapping = "Config_VesselTypeSectionDescriptionMapping";
        public const string Config_VesselTypeSectionGradingMapping = "Config_VesselTypeSectionGradingMapping";
        public const string Config_VesselTypeSubSection = "Config_VesselTypeSubSection";
        public const string Config_VesselTypeSubSectionDescriptionMapping = "Config_VesselTypeSubSectionDescriptionMapping";
        public const string Config_VesselTypeTankGradingMapping = "Config_VesselTypeTankGradingMapping";
        public const string Config_VesselTypeTankSectionMapping = "Config_VesselTypeTankSectionMapping";
        #endregion

        #region SQL schema core (21 tables)
        public const string core_AbsApplication = "core_AbsApplication";
        public const string core_ABSContacts = "core_ABSContacts";
        public const string core_AbsLog = "core_AbsLog";
        public const string core_All_Countries_Staging = "core_All_Countries_Staging";
        public const string core_All_Ports_Staging = "core_All_Ports_Staging";
        public const string core_Company = "core_Company";
        public const string core_Country = "core_Country";
        public const string core_Customer = "core_Customer";
        public const string core_Dictionary = "core_Dictionary";
        public const string core_DictionaryValue = "core_DictionaryValue";
        public const string core_Documents = "core_Documents";
        public const string core_EU_Countries_Staging = "core_EU_Countries_Staging";
        public const string core_EUTerritory = "core_EUTerritory";
        public const string core_LogAction = "core_LogAction";
        public const string core_PortOfCall = "core_PortOfCall";
        public const string core_TermsOfUse = "core_TermsOfUse";
        public const string core_TermsOfUse_Text = "core_TermsOfUse_Text";
        public const string core_UserWCN_Staging = "core_UserWCN_Staging";
        public const string core_UserWCNMapping = "core_UserWCNMapping";
        public const string core_Vessel = "core_Vessel";
        public const string core_wcn_corrections_staging = "core_wcn_corrections_staging";
        #endregion

        #region SQL schema dbo (69 tables)
        public const string dbo_ApplicationToOrganizationMappings = "dbo_ApplicationToOrganizationMappings";
        public const string dbo_AspNetRoleClaims = "dbo_AspNetRoleClaims";
        public const string dbo_AspNetRoles = "dbo_AspNetRoles";
        public const string dbo_AspNetUserClaims = "dbo_AspNetUserClaims";
        public const string dbo_AspNetUserLogins = "dbo_AspNetUserLogins";
        public const string dbo_AspNetUserRoles = "dbo_AspNetUserRoles";
        public const string dbo_AspNetUsers = "dbo_AspNetUsers";
        public const string dbo_AspNetUserTokens = "dbo_AspNetUserTokens";
        public const string dbo_BulkUpload = "dbo_BulkUpload";
        public const string dbo_Clients = "dbo_Clients";
        public const string dbo_CurrentCondition = "dbo_CurrentCondition";
        public const string dbo_GenericImageCard = "dbo_GenericImageCard";
        public const string dbo_GenericImageCard_offline = "dbo_GenericImageCard_offline";
        public const string dbo_Grading = "dbo_Grading";
        public const string dbo_Grading_bkup = "dbo_Grading_bkup";
        public const string dbo_Grading_history = "dbo_Grading_history";
        public const string dbo_Grading_offline = "dbo_Grading_offline";
        public const string dbo_GradingCondition = "dbo_GradingCondition";
        public const string dbo_GradingSectionMapping = "dbo_GradingSectionMapping";
        public const string dbo_H_IReportTemplate = "dbo_H_IReportTemplate";
        public const string dbo_H_IReportTemplate_history = "dbo_H_IReportTemplate_history";
        public const string dbo_H_IReportTemplate_offline = "dbo_H_IReportTemplate_offline";
        public const string dbo_HandIProjectGrading = "dbo_HandIProjectGrading";
        public const string dbo_HandIProjectGrading_offline = "dbo_HandIProjectGrading_offline";
        public const string dbo_HandIProjectGradingConditions = "dbo_HandIProjectGradingConditions";
        public const string dbo_HandIProjectGradingConditions_offline = "dbo_HandIProjectGradingConditions_offline";
        public const string dbo_ImageCard = "dbo_ImageCard";
        public const string dbo_ImageDescriptions = "dbo_ImageDescriptions";
        public const string dbo_ImageDescriptions_bkup = "dbo_ImageDescriptions_bkup";
        public const string dbo_ImageDescriptions_history = "dbo_ImageDescriptions_history";
        public const string dbo_ImageDescriptions_offline = "dbo_ImageDescriptions_offline";
        public const string dbo_IsSynchedOnline = "dbo_IsSynchedOnline";
        public const string dbo_ProjectGradingConditionMapping = "dbo_ProjectGradingConditionMapping";
        public const string dbo_ProjectGradingConditionMapping_history = "dbo_ProjectGradingConditionMapping_history";
        public const string dbo_ProjectGradingConditionMapping_offline = "dbo_ProjectGradingConditionMapping_offline";
        public const string dbo_ProjectGradingMapping = "dbo_ProjectGradingMapping";
        public const string dbo_ProjectGradingMapping_history = "dbo_ProjectGradingMapping_history";
        public const string dbo_ProjectGradingMapping_offline = "dbo_ProjectGradingMapping_offline";
        public const string dbo_ProjectImageCards = "dbo_ProjectImageCards";
        public const string dbo_ProjectImageCards_history = "dbo_ProjectImageCards_history";
        public const string dbo_ProjectImageCards_offline = "dbo_ProjectImageCards_offline";
        public const string dbo_ProjectTemplateMapping = "dbo_ProjectTemplateMapping";
        public const string dbo_ProjectTemplateMapping_history = "dbo_ProjectTemplateMapping_history";
        public const string dbo_ProjectTemplateMapping_offline = "dbo_ProjectTemplateMapping_offline";
        public const string dbo_ReportStatutoryCertificates = "dbo_ReportStatutoryCertificates";
        public const string dbo_ReportSurveyDetails = "dbo_ReportSurveyDetails";
        public const string dbo_ReportTemplate = "dbo_ReportTemplate";
        public const string dbo_ReportTemplateSection = "dbo_ReportTemplateSection";
        public const string dbo_ReportTemplateSection_temp = "dbo_ReportTemplateSection_temp";
        public const string dbo_ReportVesselMainData = "dbo_ReportVesselMainData";
        public const string dbo_RolePermissionsMapping = "dbo_RolePermissionsMapping";
        public const string dbo_Section_ImageDescriptionMapping = "dbo_Section_ImageDescriptionMapping";
        public const string dbo_SubSection = "dbo_SubSection";
        public const string dbo_Tank_ImageDescriptionMapping = "dbo_Tank_ImageDescriptionMapping";
        public const string dbo_TankImageCard = "dbo_TankImageCard";
        public const string dbo_TankImageCard_history = "dbo_TankImageCard_history";
        public const string dbo_TankImageCard_offline = "dbo_TankImageCard_offline";
        public const string dbo_TankType = "dbo_TankType";
        public const string dbo_TemplateSectionMapping = "dbo_TemplateSectionMapping";
        public const string dbo_TempVesselTypeSubsection = "dbo_TempVesselTypeSubsection";
        public const string dbo_UserAccounts = "dbo_UserAccounts";
        public const string dbo_UserPermissions = "dbo_UserPermissions";
        public const string dbo_Vessel_GradingMapping = "dbo_Vessel_GradingMapping";
        public const string dbo_Vessel_GradingMapping_history = "dbo_Vessel_GradingMapping_history";
        public const string dbo_Vessel_GradingMapping_offline = "dbo_Vessel_GradingMapping_offline";
        public const string dbo_Vessel_TankMapping = "dbo_Vessel_TankMapping";
        public const string dbo_Vessel_TankMapping_history = "dbo_Vessel_TankMapping_history";
        public const string dbo_Vessel_TankMapping_offline = "dbo_Vessel_TankMapping_offline";
        public const string dbo_VesselType = "dbo_VesselType";
        #endregion

        #region SQL schema er (1 tables)
        public const string er_EmissionReportingPortalCustomerContacts = "er_EmissionReportingPortalCustomerContacts";
        #endregion

        #region SQL schema upskill (9 tables)
        public const string upskill_CapLog = "upskill_CapLog";
        public const string upskill_ImageData = "upskill_ImageData";
        public const string upskill_ImageData_history = "upskill_ImageData_history";
        public const string upskill_ImageData_offline = "upskill_ImageData_offline";
        public const string upskill_Images = "upskill_Images";
        public const string upskill_Images_history = "upskill_Images_history";
        public const string upskill_Images_offline = "upskill_Images_offline";
        public const string upskill_ReportTemplates = "upskill_ReportTemplates";
        public const string upskill_ReportTemplateSections = "upskill_ReportTemplateSections";
        #endregion

        #region Extra (1 container)
        public const string sys_Counters = "sys_Counters";
        #endregion

        public static readonly IReadOnlyDictionary<string, string> PartitionKeys = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>
            {
                #region SQL schema CAP (25 tables)
                { CAP_Activities, "/ProjectId" },
                { CAP_CAPCoordinators, "/id" },
                { CAP_Classes, "/id" },
                { CAP_Clients, "/id" },
                { CAP_Comments, "/ProjectId" },
                { CAP_Comments_history, "/ProjectId" },
                { CAP_Comments_offline, "/ProjectId" },
                { CAP_Files, "/id" },
                { CAP_Lookups, "/id" },
                { CAP_ProjectGrades, "/ProjectId" },
                { CAP_Projects, "/id" },
                { CAP_Projects_history, "/id" },
                { CAP_Projects_offline, "/id" },
                { CAP_ProjectTaskStatus, "/ProjectId" },
                { CAP_Reports, "/id" },
                { CAP_Statuses, "/id" },
                { CAP_SurveyImages, "/ProjectId" },
                { CAP_TaskDueDates, "/id" },
                { CAP_Tasks, "/id" },
                { CAP_TaskStatusGroups, "/id" },
                { CAP_TemplateGradingSectionPhotoDescriptions, "/id" },
                { CAP_TemplateGradingSections, "/id" },
                { CAP_TemplateGradingSectionSurveyItems, "/id" },
                { CAP_Templates, "/id" },
                { CAP_TemplateSections, "/TemplateId" },
                #endregion

                #region SQL schema Config (16 tables)
                { Config_H_I_Grading, "/id" },
                { Config_ProjectSectionDescriptionMapping, "/id" },
                { Config_ProjectSectionGradingMapping, "/id" },
                { Config_ProjectSectionMapping, "/ProjectId" },
                { Config_ProjectSubSection, "/id" },
                { Config_ProjectSubSectionDescriptionMapping, "/id" },
                { Config_ProjectTankSectionMapping, "/ProjectId" },
                { Config_ReportPart, "/ProjectId" },
                { Config_VesselTypeNormalSectionMapping, "/VesselTypeId" },
                { Config_VesselTypePartMapping, "/VesselTypeId" },
                { Config_VesselTypeSectionDescriptionMapping, "/id" },
                { Config_VesselTypeSectionGradingMapping, "/id" },
                { Config_VesselTypeSubSection, "/id" },
                { Config_VesselTypeSubSectionDescriptionMapping, "/id" },
                { Config_VesselTypeTankGradingMapping, "/id" },
                { Config_VesselTypeTankSectionMapping, "/VesselTypeId" },
                #endregion

                #region SQL schema core (21 tables)
                { core_AbsApplication, "/id" },
                { core_ABSContacts, "/id" },
                { core_AbsLog, "/id" },
                { core_All_Countries_Staging, "/id" },
                { core_All_Ports_Staging, "/id" },
                { core_Company, "/id" },
                { core_Country, "/id" },
                { core_Customer, "/id" },
                { core_Dictionary, "/id" },
                { core_DictionaryValue, "/id" },
                { core_Documents, "/VesselId" },
                { core_EU_Countries_Staging, "/id" },
                { core_EUTerritory, "/id" },
                { core_LogAction, "/id" },
                { core_PortOfCall, "/id" },
                { core_TermsOfUse, "/id" },
                { core_TermsOfUse_Text, "/id" },
                { core_UserWCN_Staging, "/id" },
                { core_UserWCNMapping, "/id" },
                { core_Vessel, "/id" },
                { core_wcn_corrections_staging, "/id" },
                #endregion

                #region SQL schema dbo (69 tables)
                { dbo_ApplicationToOrganizationMappings, "/id" },
                { dbo_AspNetRoleClaims, "/id" },
                { dbo_AspNetRoles, "/id" },
                { dbo_AspNetUserClaims, "/id" },
                { dbo_AspNetUserLogins, "/id" },
                { dbo_AspNetUserRoles, "/id" },
                { dbo_AspNetUsers, "/id" },
                { dbo_AspNetUserTokens, "/id" },
                { dbo_BulkUpload, "/id" },
                { dbo_Clients, "/ClientId" },
                { dbo_CurrentCondition, "/id" },
                { dbo_GenericImageCard, "/ProjectId" },
                { dbo_GenericImageCard_offline, "/ProjectId" },
                { dbo_Grading, "/ProjectId" },
                { dbo_Grading_bkup, "/ProjectId" },
                { dbo_Grading_history, "/ProjectId" },
                { dbo_Grading_offline, "/ProjectId" },
                { dbo_GradingCondition, "/id" },
                { dbo_GradingSectionMapping, "/id" },
                { dbo_H_IReportTemplate, "/ProjectId" },
                { dbo_H_IReportTemplate_history, "/ProjectId" },
                { dbo_H_IReportTemplate_offline, "/ProjectId" },
                { dbo_HandIProjectGrading, "/ProjectID" },
                { dbo_HandIProjectGrading_offline, "/ProjectID" },
                { dbo_HandIProjectGradingConditions, "/ProjectId" },
                { dbo_HandIProjectGradingConditions_offline, "/ProjectId" },
                { dbo_ImageCard, "/id" },
                { dbo_ImageDescriptions, "/ProjectId" },
                { dbo_ImageDescriptions_bkup, "/ProjectId" },
                { dbo_ImageDescriptions_history, "/ProjectId" },
                { dbo_ImageDescriptions_offline, "/ProjectId" },
                { dbo_IsSynchedOnline, "/ProjectId" },
                { dbo_ProjectGradingConditionMapping, "/ProjectId" },
                { dbo_ProjectGradingConditionMapping_history, "/ProjectId" },
                { dbo_ProjectGradingConditionMapping_offline, "/ProjectId" },
                { dbo_ProjectGradingMapping, "/ProjectId" },
                { dbo_ProjectGradingMapping_history, "/ProjectId" },
                { dbo_ProjectGradingMapping_offline, "/ProjectId" },
                { dbo_ProjectImageCards, "/ProjectId" },
                { dbo_ProjectImageCards_history, "/ProjectId" },
                { dbo_ProjectImageCards_offline, "/ProjectId" },
                { dbo_ProjectTemplateMapping, "/ProjectId" },
                { dbo_ProjectTemplateMapping_history, "/ProjectId" },
                { dbo_ProjectTemplateMapping_offline, "/ProjectId" },
                { dbo_ReportStatutoryCertificates, "/id" },
                { dbo_ReportSurveyDetails, "/id" },
                { dbo_ReportTemplate, "/id" },
                { dbo_ReportTemplateSection, "/id" },
                { dbo_ReportTemplateSection_temp, "/id" },
                { dbo_ReportVesselMainData, "/id" },
                { dbo_RolePermissionsMapping, "/id" },
                { dbo_Section_ImageDescriptionMapping, "/id" },
                { dbo_SubSection, "/id" },
                { dbo_Tank_ImageDescriptionMapping, "/id" },
                { dbo_TankImageCard, "/ProjectId" },
                { dbo_TankImageCard_history, "/ProjectId" },
                { dbo_TankImageCard_offline, "/ProjectId" },
                { dbo_TankType, "/TemplateId" },
                { dbo_TemplateSectionMapping, "/TemplateId" },
                { dbo_TempVesselTypeSubsection, "/id" },
                { dbo_UserAccounts, "/ClientId" },
                { dbo_UserPermissions, "/id" },
                { dbo_Vessel_GradingMapping, "/ProjectId" },
                { dbo_Vessel_GradingMapping_history, "/ProjectId" },
                { dbo_Vessel_GradingMapping_offline, "/ProjectId" },
                { dbo_Vessel_TankMapping, "/ProjectId" },
                { dbo_Vessel_TankMapping_history, "/id" },
                { dbo_Vessel_TankMapping_offline, "/ProjectId" },
                { dbo_VesselType, "/id" },
                #endregion

                #region SQL schema er (1 tables)
                { er_EmissionReportingPortalCustomerContacts, "/id" },
                #endregion

                #region SQL schema upskill (9 tables)
                { upskill_CapLog, "/id" },
                { upskill_ImageData, "/id" },
                { upskill_ImageData_history, "/id" },
                { upskill_ImageData_offline, "/ProjectId" },
                { upskill_Images, "/id" },
                { upskill_Images_history, "/id" },
                { upskill_Images_offline, "/ProjectId" },
                { upskill_ReportTemplates, "/id" },
                { upskill_ReportTemplateSections, "/id" },
                #endregion

                #region Extra (1 container)
                { sys_Counters, "/id" }
                #endregion
            });
    }
}
