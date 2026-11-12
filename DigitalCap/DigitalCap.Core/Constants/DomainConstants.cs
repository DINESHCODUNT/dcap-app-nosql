using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Constants
{
    public class DomainConstants
    {
        public static class ConditionColr
        {
            public static string Green = "Green";
            public static string Yellow = "Yellow";
            public static string Red = "Red";
        }

        public static class HullCriteria
        {
            public static string Cleanliness = "Cleanliness";
            public static string Coating = "Coating";
            public static string Corrosion = "Corrosion";
            public static string Deformation = "Deformation";
            public static string Fracture = "Fracture";
            public static string PittingGrooving = "PittingGrooving";
        }

        public static class MachineryCriteria
        {
            public static string AnomalyDetection = "AnomalyDetection";
            public static string ClassStatutoryFinding = "ClassStatutoryFinding";
            public static string ConditionMonitoring = "ConditionMonitoring";
            public static string ConditionMonitoringClient = "ConditionMonitoringClient";
            public static string Failure = "Failure";
            public static string PlannedMaintenance = "PlannedMaintenance";
            public static string PlannedMaintenanceClient = "PlannedMaintenanceClient";
            public static string RAMS = "RAMS";
            public static string SurveyAssessment = "SurveyAssessment";
        }

        public static class MachineryCriteriaId
        {
            public static Guid AnomalyDetection = new Guid("2A3FAC7F-63ED-EA11-AF8C-28187804AFF1");
            public static Guid ClassStatutoryFinding = new Guid("5128C378-63ED-EA11-AF8C-28187804AFF1");
            public static Guid ConditionMonitoring = new Guid("3D78E468-63ED-EA11-AF8C-28187804AFF1");
            public static Guid Failure = new Guid("57727587-63ED-EA11-AF8C-28187804AFF1");
            public static Guid PlannedMaintenance = new Guid("A2EEE25F-63ED-EA11-AF8C-28187804AFF1");
            public static Guid RAMS = new Guid("293FAC7F-63ED-EA11-AF8C-28187804AFF1");
            public static Guid SurveyAssessment = new Guid("1FA167EF-F9ED-EA11-AF8C-28187804AFF1");
        }


        public static class ConditionStatus
        {
            public static string Good = "Good";
            public static string Fair = "Fair";
            public static string Poor = "Poor";
        }

        public static class ConditionScore
        {
            public static string Green = "Green";
            public static string Yellow = "Yellow";
            public static string Red = "Red";
        }

        public static class AnomalylStatus
        {
            public static Guid Created = new Guid("D76F5566-D6C7-EA11-8B03-0003FFD551B2");
            public static Guid New = new Guid("D86F5566-D6C7-EA11-8B03-0003FFD551B2");
            public static Guid Open = new Guid("BAF09771-D6C7-EA11-8B03-0003FFD551B2");
            public static Guid Closed = new Guid("BBF09771-D6C7-EA11-8B03-0003FFD551B2");
        }

        public static class InspectionCategory
        {
            public static Guid Hull = new Guid("49EAE853-32CE-EA11-8B03-0003FFD551B2");
            public static Guid Machinery = new Guid("4AEAE853-32CE-EA11-8B03-0003FFD551B2");
            public static Guid Component = new Guid("39BA91DE-A3DE-EA11-AF8C-501AC57C791D");
        }

        public static class InspectionStatus
        {
            public static Guid New = new Guid("2693CB8A-D6C7-EA11-8B03-0003FFD551B2");
            public static Guid Open = new Guid("2793CB8A-D6C7-EA11-8B03-0003FFD551B2");
            public static Guid Submitted = new Guid("3EF9039C-D6C7-EA11-8B03-0003FFD551B2");
            public static Guid Approved = new Guid("08FFD2B0-D491-45D6-9E07-9F50F3E77EFC");
            public static Guid Rejected = new Guid("CEBDF14D-AD72-4DE7-A3A6-B3905228ACEA");
        }

        public static class RiskLevel
        {
            public static Guid None = new Guid("887A4E49-64C1-EA11-9B05-00155DFC1F68");
            public static Guid Low = new Guid("9AC8BF5E-F0BA-EA11-9B05-28187891C181");
            public static Guid MediumLow = new Guid("7B66E876-F0BA-EA11-9B05-28187891C181");
            public static Guid Medium = new Guid("480B6168-F0BA-EA11-9B05-28187891C181");
            public static Guid MediumHigh = new Guid("7FEEE67D-F0BA-EA11-9B05-28187891C181");
            public static Guid High = new Guid("9BC8BF5E-F0BA-EA11-9B05-28187891C181");
            public static Guid VeryHigh = new Guid("897A4E49-64C1-EA11-9B05-00155DFC1F68");
        }

        public static class ColorCode
        {
            public static Guid Green = new Guid("2C73C13C-F0BA-EA11-9B05-28187891C181");
            public static Guid Yellow = new Guid("1DAD7645-F0BA-EA11-9B05-28187891C181");
            public static Guid Red = new Guid("2B73C13C-F0BA-EA11-9B05-28187891C181");
        }

        public static class TableNames
        {
            public static string SURVEY_TYPES = "SurveyTypes";
            public static string EVENTS = "Events";
            public static string MACHINERY = "Machinery";
            public static string SYSTEMS = "Systems";
            public static string INSPECTIONS = "Inspections";
            public static string DICTIONARYVALUES = "DictionaryValues";
            public static string INSPECTION_TARGETS = "InspectionTargets";
            public static string AliasTargets = "targs";
            public static string AliasInspections = "insps";
            public static string AliasEvents = "ev";
            public static string Alerts = "Alerts";
            public static string AliasDictionaryValues = "dv";
            public static string InspectionCriteriaSnapshots = "InspectionCriteriaSnapshots";
            public static string InspectionScoreSnapshots = "InspectionScoreSnapshots";

        }

        public static class StoredProcedures
        {

            public static class DictionaryValues
            {
                public static class CompartmentTypes
                {
                    public static readonly string ByVesselTypeId = "sp_CompartmentFunctions_GetByVesselTypeId";
                }


                public static readonly string GetFunctionsByVessel = "sp_DictionaryValues_GetFunctionsByVessel";

            }

            public static class Events
            {
                public static readonly string GetEventsByInspection = "sp_Events_GetEventsByInspection";
                public static readonly string GetEventInspector = "sp_Events_GetEventsInspectors";

            }

            public static class InspectionScores
            {
                public static readonly string GetConditionScores = "sp_InspectionScores_GetConditionScores";
                public static readonly string GetPCMScores = "sp_InspectionScores_GetPCMScores";
            }

            public static class InspectionMachinerySummary
            {
                public static readonly string GetByVessel = "sp_InspectionMachinerySummary_GetByVessel";
            }

            public static class InspectionMachineryComponents
            {
                public static readonly string GetByMachinery = "sp_InspectionMachineryComponents_GetByMachinery";
                public static readonly string GetLastApprovedByMachinery = "sp_InspectionMachineryComponents_GetLastApprovedByMachinery";
                public static readonly string GetForComponent = "sp_InspectionMachineryComponents_GetForComponent";
                public static readonly string GetByInspection = "sp_InspectionMachineryComponents_GetByInspection";
                public static readonly string GetForComponentByInspection = "sp_InspectionMachineryComponents_GetForComponentByInspection";
                public static readonly string GetForMachineryByInspection = "sp_InspectionMachineryComponents_GetForMachineryByInspection";
            }

            public static class InspectionHullSummary
            {
                public static readonly string GetForCompartment = "sp_InspectionHullSummary_GetForCompartment";
                public static readonly string GetByVessel = "sp_InspectionHullSummary_GetByVessel";
            }

            public static class InspectionZones
            {
                public static readonly string GetZonesByCompartment = "sp_InspectionZones_GetZonesByCompartment";
                public static readonly string GetLastApprovedByCompartment = "sp_InspectionZones_GetLastApprovedByCompartment";
                public static readonly string GetLastApprovedByVessel = "sp_InspectionZones_GetLastApprovedByVessel";
            }

            public static class CriticalAreas
            {
                public static readonly string GetByInspection = "sp_CriticalAreas_GetByInspection";
                public static readonly string GetByCompartment = "sp_CriticalAreas_GetByCompartment";
            }

            public static class InspectionCriticalArea
            {
                public static readonly string GetByInspection = "sp_InspectionCriticalArea_GetByInspection";
                public static readonly string GetByZone = "sp_InspectionCriticalArea_GetByZone";
            }

            public static class Inspections
            {
                public static readonly string GetForCompartment = "sp_Inspections_GetForCompartment";
                public static readonly string GetEngineeringAnalysis = "sp_Inspections_GetEngineeringAnalysis";
                public static readonly string GetConditionScores = "sp_Inspections_GetConditionScores";
                public static readonly string GetTrendsForCompartment = "sp_Inspections_GetTrendsForCompartment";
                public static readonly string GetTrendsForComponent = "sp_Inspections_GetTrendsForComponent";
                public static readonly string GetTrendsForMachinery = "sp_Inspections_GetTrendsForMachinery";
                public static readonly string GetCountByVessel = "sp_Inspections_GetCountByVessel";
                public static readonly string GetVesselPerformance = "sp_Inspections_GetVesselPerformance";
                public static readonly string GetVesselPerformanceForMachinery = "sp_Inspections_GetVesselPerformanceForMachinery";
                public static readonly string GetCompartmentCriteriaByVessel = "sp_Inspections_GetCompartmentCriteriaByVessel";
                public static readonly string GetInspectionCount = "sp_DashboardGetCounts";
                public static readonly string GetTargets = "sp_Inspections_GetTargets";
                public static readonly string GetById = "sp_Inspections_GetById";

                public static readonly string GetDashboardData = "sp_Inspection_DashboardData";
                public static readonly string GetInspectionSummaryDashboardData = "sp_Inspection_SummaryDashboardData";

            }

            public static class Polygons
            {
                public static readonly string Insert = "sp_Polygons_Insert";
                public static readonly string GetById = "sp_Polygons_GetById";
                public static readonly string GetByVessel = "sp_Polygons_GetByVessel";
                public static readonly string GetByCompartment = "sp_Polygons_GetByCompartment";
                public static readonly string AddToCompartment = "sp_Polygons_AddToCompartment";
                public static readonly string RemoveFromCompartment = "sp_Polygons_RemoveFromCompartment";
                public static readonly string AddToVessel = "sp_Polygons_AddToVessel";
                public static readonly string RemoveFromVessel = "sp_Polygons_RemoveFromVessel";
                public static readonly string GetPolyCompModelForCompartments = "sp_Polygons_GetForCompartments";
                public static readonly string GetForVesselCompartments = "sp_Polygons_GetForVesselCompartments";
                public static readonly string GetForVessel = "sp_Polygons_GetForVessel";
            }

            public static class Frames
            {
                public static readonly string GetByVessel = "sp_Frames_GetByVessel";
            }

            public static class Email
            {
                public static string GetEmailByRequestId = "sp_Get_Email_By_RequestId";
                public static string GetResetPasswordTemplate = "sp_Get_ResetPassword_Template";

                public static class Template
                {
                    public static string GetByType = "sp_EmailTemplates_GetByType";
                }
            }

            public static class InspectionCriteria
            {
                public static readonly string GetByType = "sp_InspectionCriteria_GetByType";
                public static readonly string GetPrimary = "sp_InspectionCriteria_GetPrimary";
                public static readonly string RemoveFromVessel = "sp_InspectionCriteria_RemoveFromVessel";
                public static readonly string GetVesselWithCriteria = "sp_InspectionCriteria_GetVessels";
                public static readonly string GetHullRepairTypeCriteria = "sp_Anomalies_GetHull_RepairTypeCriteria";
            }

            public static class UserAccounts
            {
                public static readonly string GetByAspNetId = "sp_UserAccounts_GetByAspNetId";
                public static readonly string GetUsersInRole = "sp_UserAccounts_GetUsersInRole";
                public static readonly string GetUsersWithPermission = "sp_UserAccounts_GetUsersWithPermission";
            }

            public static class Documents
            {
                public static readonly string GetByVesselID = "sp_Documents_GetByVesselID";
                public static readonly string GetByInspectionId = "sp_Documents_GetByInspectionId";
            }

            public static class DefectDescriptor
            {
                public static readonly string AddCriteria = "sp_DefectDescriptor_AddCriteria";
                public static readonly string RemoveCriteria = "sp_DefectDescriptor_RemoveCriteria";
                public static readonly string GetForHull = "sp_DefectDescriptor_GetForHull";
                public static readonly string GetForMachinery = "sp_DefectDescriptor_GetForMachinery";
            }

            public static class RepairType
            {
                public static readonly string AddCriteria = "sp_RepairType_AddCriteria";
                public static readonly string RemoveCriteria = "sp_RepairType_RemoveCriteria";
                public static readonly string FromAnomaly = "sp_RepairType_FromAnomaly";
            }

            public static class AffectedStructure
            {
                public static readonly string AddVessetType = "sp_AffectedStructure_AddVesselType";
                public static readonly string RemoveVessetTypes = "sp_AffectedStructure_RemoveVesselType";
            }

            public static class Anomalies
            {
                public static readonly string GetPerformanceForMachinery = "sp_Anomalies_GetPerformanceForMachinery";
                public static readonly string GetPerformanceForHull = "sp_Anomalies_GetPerformanceForHull";
                public static readonly string GetAffectedStructures = "sp_Anomalies_GetAffectedStructures";
                public static readonly string GetCriticalAreas = "sp_Anomalies_GetCriticalAreas";
                public static readonly string GetById = "sp_Anomalies_GetById";
                public static readonly string GetByIdForMachinery = "sp_Anomalies_GetByIdForMachinery";
                public static readonly string GetByIdForComponent = "sp_Anomalies_GetByIdForComponent";
                public static readonly string GetByInspectionId = "sp_Anomalies_GetByInspectionId";
                public static readonly string GetForMachinery = "sp_Anomalies_GetForMachinery";
                public static readonly string GetForComponent = "sp_Anomalies_GetForComponent";
                public static readonly string GetStatusCounts = "sp_Anomalies_GetStatusCounts";
                public static readonly string GetCountByVessel = "sp_Anomalies_GetCountByVessel";
                public static readonly string GetOpenAnomaliesByVessel = "sp_Anomalies_GetOpenAnomaliesByVessel";
                public static readonly string GetOpenAnomaliesForMachineryByVessel = "sp_Anomalies_GetOpenAnomaliesForMachineryByVessel";
                public static readonly string GetRepairCountByVessel = "sp_Anomalies_GetRepairCountByVessel";
                public static readonly string GetAnomaliesForDashboard = "sp_Anomalies_GetForDashboard";
                public static readonly string GetHullAnomaliesWithPointValuesAbove = "sp_Anomalies_WithPointValuesAbove_Hull";
                public static readonly string GetMachineryAnomaliesWithPointValuesAbove = "sp_Anomalies_WithPointValuesAbove_Machinery";
                public static readonly string GetAnomalies = "sp_Anomalies_GetAnomalies";

                public static readonly string GetDashboardData = "sp_Anomalies_DashboardData";
                public static readonly string Tile_AnomaliesByScore = "sp_Anomalies_Tile_AnomaliesbyScore";
                public static readonly string Tile_AnomaliesByRiskLevel = "sp_Anomalies_Tile_AnomaliesbyRiskLevel";
            }

            public static class AnomalyCriticalArea
            {
                public static readonly string GetAnomalies = "sp_AnomalyCriticalAreas_GetAnomalies";
            }

            public static class Zone
            {
                public static readonly string GetByInspection = "sp_Zones_GetByInspection";
                public static readonly string GetByCompartment = "sp_Zones_GetByCompartment";
                public static readonly string GetByVessel = "sp_Zones_GetByVessel";
            }

            public static class Machinery
            {
                public static readonly string GetByInspection = "sp_Machinery_GetByInspection";
                public static readonly string GetByVessel = "sp_Machinery_GetByVessel";
                public static readonly string GetStructureProfileByVessel = "sp_Machinery_GetStructureProfileByVessel";
            }

            public static class Compartment
            {
                public static readonly string GetByVessel = "sp_Compartments_GetByVessel";
            }

            public static class Component
            {
                public static readonly string GetByInspection = "sp_Components_GetByInspection";
                public static readonly string GetByVessel = "sp_Components_GetByVessel";
            }

            public static class Vessel
            {
                public static readonly string GetCompartmentAverageScores = "sp_Vessel_GetCompartmentAverageScores";
                public static readonly string GetCompartmentMaxScores = "sp_Vessel_GetCompartmentMaxScores";
                public static readonly string GetCompartmentsNotCompleted = "sp_Compartments_Not_Completed";
                public static readonly string GetMachineryConditionScores = "sp_Vessel_GetMachineryConditionScores";
                public static readonly string GetHullConditionSummary = "sp_Vessels_GetHullConditionSummary";
                public static readonly string GetHullWeightedConditionSummary = "sp_Vessels_GetHullWeightedConditionSummary";
                public static readonly string GetMachineryConditionSummary = "sp_Vessels_GetMachineryConditionSummary";
                public static readonly string GetMachineryConditionStatusSummary = "sp_Vessels_GetMachineryConditionStatusSummary";
                public static readonly string GetCriticalAreaScores = "sp_Vessels_GetCriticalAreaScores";
            }

            public static class Alerts
            {
                public static readonly string UpdateStatus = "sp_Alerts_UpdateStatus";
                public static readonly string GetTotalsByCategory = "sp_Alerts_GetTotalsByCategory";
                public static readonly string GetSummaryAlert = "sp_Alerts_GetSummaryAlert";
                public static readonly string GetSummaryAlertsByCategory = "sp_Alerts_GetSummaryAlertsByCategory";
                public static readonly string GetAlertsOnOrAfterDate = "sp_Alerts_GetAlertsOnOrAfterDate";
                public static readonly string GetAlertsByCategory = "sp_Alerts_GetAlertsByCategory";

                public static class StructuralDashboard
                {
                    public static readonly string GetByAlertId = "sp_StructuralDashboardAlerts_GetByAlertId";
                }
            }

            public static class EquipmmentScores
            {
                public static readonly string GetByMachineryOrComponent = "sp_EquipmentScores_GetByMachineryOrComponent";
            }

        }

        public static class Events
        {
            public static string EVENT = "Event";
            public static string ASSET = "Asset";

        }

        public static class Email
        {

            public static class Templates
            {

                public static class Fields
                {
                    public static readonly string Delimiter = "#";
                    public static readonly string UserName = "USERNAME";
                    public static readonly string ActionName = "ACTIONNAME";
                    public static readonly string ActionDescription = "ACTIONDESC";
                    public static readonly string ConditionManagerLink = "LINKBACKTOCONDITIONMANAGER";
                }
            }
        }

        public static class Errors
        {
            public static class Views
            {
                public static readonly string GeneralErrorFieldName = "General";
            }
        }

    }
}
