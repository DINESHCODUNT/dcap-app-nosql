using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Transactions;

namespace DigitalCap.Persistence.Repositories
{
    public class ReportPartRepository : IReportPartRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;
        public ReportPartRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        public void Commit()
        {
            _unitOfWork?.Commit();
        }
        public async Task<bool> CreateProjectReportTemplate(int vesselTypeId, string userName, int projectId, bool imoExists = false, int copyprojectId = 0)
        {
            try
            {
                var result = await Connection.ExecuteAsync(
                      sql: "[Config].[CreateProject_Templates]",
                 commandType: CommandType.StoredProcedure,
                     param: new
                     {
                         VesselTypeId = vesselTypeId,
                         ProjectId = projectId,
                         UserName = userName,
                         IMOExists = imoExists,
                         CopyProjectId = copyprojectId
                     },
                 transaction: Transaction);
                this.Commit();

                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }
        public async Task<ReportConfigDropDownDataList> GetVesselTypes()
        {
            try
            {
                var vesselTypes = await Connection.QueryAsync<VesselTypes>(
                    sql: "[Config].[Get_VesselTypes]",
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                return new ReportConfigDropDownDataList
                {
                    vesselTypes = vesselTypes.ToList()
                };
            }
            catch
            {
                return null;
            }
        }
        //reportpart
        public async Task<ReportConfigDropDownDataList> GetReportPartsByVesselTypeAsync(int vesselTypeId)
        {
            try
            {
                var reportTemplates = await Connection.QueryAsync<ReportTemplates>(
                    sql: "[Config].[Get_ReportPartsByVesselType]",
                    param: new { VesselTypeId = vesselTypeId },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                return new ReportConfigDropDownDataList
                {
                    reportTemplates = reportTemplates.ToList()
                };
            }
            catch
            {
                return null;
            }
        }

        //reportpart
        public async Task<PartSectionNamesList> GetSectionNamesByPartNameAsync(int vesselTypeId, string partName)
        {
            try
            {
                using (var multi = await Connection.QueryMultipleAsync(
                    sql: "[Config].[Get_SectionNamesByPartName]",
                    param: new
                    {
                        VesselTypeId = vesselTypeId,
                        PartName = partName
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                ))
                {
                    return new PartSectionNamesList
                    {
                        NormalSections = (await multi.ReadAsync<NormalSectionNames>()).ToList(),
                        TanksSections = (await multi.ReadAsync<TankSectionNames>()).ToList()
                    };
                }
            }
            catch
            {
                return null;
            }
        }
        //reportpart
        public async Task<PartSectionNamesList> GetSectionNamesByPartId(int vesselTypeId, int partId)
        {
            try
            {
                using (var multi = await Connection.QueryMultipleAsync(
                    sql: "[config].[GetVesselTypeSectionNormalMapping]",
                    param: new
                    {
                        PartId = partId,
                        VesselTypeId = vesselTypeId
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                ))
                {
                    return new PartSectionNamesList
                    {
                        NormalSections = (await multi.ReadAsync<NormalSectionNames>()).ToList(),
                        SubSections = (await multi.ReadAsync<SubSectionNames>()).ToList(),
                        TanksSections = (await multi.ReadAsync<TankSectionNames>()).ToList()
                    };
                }
            }
            catch
            {
                return null;
            }
        }
        //reportpart
        public async Task<bool> CreateReportPart(VesselTypeReportConfigList reportConfigList, int vesselTypeId, string modifiedBy)
        {
            try
            {
                var dtParts = BuildReportParts(reportConfigList.reportParts, vesselTypeId, modifiedBy);
                var dtSections = BuildNormalSections(reportConfigList.normalSectionMappings, modifiedBy);
                var dtTankSections = BuildTankSections(reportConfigList.tankSectionMappings, modifiedBy);
                var dtSubSections = BuildSubSections(reportConfigList.normalSubSectionMappings, modifiedBy);

                await Connection.ExecuteAsync(
                    sql: "[Config].[Create_VesseltypeReportConfig]",
                    param: new
                    {
                        VesselTypeId = vesselTypeId,
                        ReportParts = dtParts.AsTableValuedParameter("[Config].[VesselTypePartMappingTbl]"),
                        NormalSectionMappings = dtSections.AsTableValuedParameter("[Config].[VesselTypeNormalSectionMappingTbl]"),
                        TankSectionMappings = dtTankSections.AsTableValuedParameter("[Config].[VesselTypeTankSectionMappingTbl]"),
                        SubSectionMappings = dtSubSections.AsTableValuedParameter("[Config].[VesselTypeSubSectionMappingTbl]")
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private DataTable BuildReportParts(
    IEnumerable<VesselTypePartMapping>? parts,
    int vesselTypeId,
    string modifiedBy)
        {
            var dt = new DataTable();

            dt.Columns.Add("VesselTypePartMappingId", typeof(int));
            dt.Columns.Add("VesselTypeId", typeof(int));
            dt.Columns.Add("PartName", typeof(string));
            dt.Columns.Add("SequenceNo", typeof(int));
            dt.Columns.Add("IsActive", typeof(bool));
            dt.Columns.Add("CreatedBy", typeof(string));
            dt.Columns.Add("IsDeleted", typeof(bool));
            dt.Columns.Add("ModifiedBy", typeof(string));

            if (parts == null || !parts.Any())
                return dt;  

            foreach (var p in parts)
            {
                dt.Rows.Add(
      p.vesselTypePartMappingId == 0 ? DBNull.Value : p.vesselTypePartMappingId,
      vesselTypeId,
                    p.PartName,
                    p.SequenceNo,
                    p.IsActive,
      string.IsNullOrEmpty(p.CreatedBy) ? modifiedBy : p.CreatedBy,
      p.IsDeleted ?? false,
                    modifiedBy
                );

            }

            return dt;
        }

        private DataTable BuildNormalSections(IEnumerable<VesselTypeSectionMapping>? sections, string modifiedBy)
        {
            var dt = new DataTable();
            dt.Columns.Add("VesselTypeNormalSectionMappingId", typeof(Guid));
            dt.Columns.Add("VesselTypePartMappingId", typeof(int));
            dt.Columns.Add("VesselTypeId", typeof(int));
            dt.Columns.Add("PartName", typeof(string));
            dt.Columns.Add("SectionName", typeof(string));
            dt.Columns.Add("SubHeader", typeof(string));
            dt.Columns.Add("TotalCards", typeof(int));
            dt.Columns.Add("PlaceholderCount", typeof(int));
            dt.Columns.Add("FileNameCount", typeof(int));
            dt.Columns.Add("IsActive", typeof(bool));
            dt.Columns.Add("CreatedBy", typeof(string));
            dt.Columns.Add("IsDeleted", typeof(bool));
            dt.Columns.Add("ModifiedBy", typeof(string));

            if (sections == null || !sections.Any())
            {
                dt.Rows.Add(dt.NewRow());
                return dt;
            }

            foreach (var s in sections)
            {
                dt.Rows.Add(
                    s.VesselTypeNormalSectionMappingId ?? Guid.Empty,
                    s.VesselTypePartMappingId,
                    s.VesselTypeId,
                    s.PartName,
                    s.SectionName,
                    s.SubHeader,
                    s.TotalCards,
                    s.PlaceholderCount,
                    s.FileNameCount,
                    s.IsActive,
                    s.CreatedBy,
                    s.IsDeleted,
                    modifiedBy
                );
            }

            return dt;
        }

        private DataTable BuildTankSections(
          IEnumerable<VesselTypeTankSectionMapping>? tanks,
          string modifiedBy)
        {
            var dt = new DataTable();

            dt.Columns.Add("VesselTypePartMappingId", typeof(int));
            dt.Columns.Add("VesselTypeId", typeof(int));
            dt.Columns.Add("PartName", typeof(string));
            dt.Columns.Add("TankTypeId", typeof(int));
            dt.Columns.Add("TotalCards", typeof(int));
            dt.Columns.Add("PlaceholderCount", typeof(int));
            dt.Columns.Add("FileNameCount", typeof(int));
            dt.Columns.Add("IsActive", typeof(bool));
            dt.Columns.Add("CreatedBy", typeof(string));
            dt.Columns.Add("IsDeleted", typeof(bool));
            dt.Columns.Add("ModifiedBy", typeof(string));

            if (tanks == null || !tanks.Any())
            {
                dt.Rows.Add(dt.NewRow());
                return dt;
            }

            foreach (var t in tanks)
            {
                dt.Rows.Add(
                    t.VesselTypePartMappingId == 0 ? DBNull.Value : t.VesselTypePartMappingId,
                    t.VesselTypeId == 0 ? DBNull.Value : t.VesselTypeId,
                    string.IsNullOrWhiteSpace(t.PartName) ? DBNull.Value : t.PartName,
                    t.TankTypeId,
                    t.TotalCards,
                    t.PlaceholderCount,
                    t.FileNameCount,
                    t.IsActive,
                    t.CreatedBy ?? modifiedBy,
                    t.IsDeleted,
                    modifiedBy
                );
            }

            return dt;
        }

        private DataTable BuildSubSections(IEnumerable<VesselTypeSubSectionMapping>? subSections, string modifiedBy)
        {
            var dt = new DataTable();
            dt.Columns.Add("SubSectionId", typeof(Guid));
            dt.Columns.Add("SectionId", typeof(Guid));
            dt.Columns.Add("SectionName", typeof(string));
            dt.Columns.Add("TotalCards", typeof(int));
            dt.Columns.Add("PlaceholderCount", typeof(int));
            dt.Columns.Add("FileNameCount", typeof(int));
            dt.Columns.Add("IsActive", typeof(bool));
            dt.Columns.Add("IsDeleted", typeof(bool));
            dt.Columns.Add("CreatedBy", typeof(string));
            dt.Columns.Add("ModifiedBy", typeof(string));

            if (subSections == null || !subSections.Any())
            {
                dt.Rows.Add(dt.NewRow());
                return dt;
            }

            foreach (var s in subSections)
            {
                dt.Rows.Add(
                    s.SubSectionId,
                    s.SectionId,
                    s.SectionName,
                    s.TotalCards,
                    s.PlaceholderCount,
                    s.FileNameCount,
                    s.IsActive,
                    s.IsDeleted,
                    s.CreatedBy,
                    modifiedBy
                );
            }

            return dt;
        }

        public async Task<bool> CheckToShowSectionExportConfig(int projectId, int imoNumber, int partId)
        {
            try
            {
                var result = await Connection.ExecuteScalarAsync<int>(
                    sql: "Config.CheckToShowSectionExportConfig",
                    param: new
                    {
                        ProjectId = projectId,
                        ImoNumber = imoNumber,
                        PartId = partId
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                return result == 1;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateGenericImageCard(GenericImageCard card)
        {
            try
            {
                await Connection.ExecuteAsync(
                    sql: "[config].[UpdateGenericImageCard]",
                    param: new
                    {
                        card.Id,
                        card.ProjectId,
                        card.TemplateId,
                        card.SectionId,
                        card.CardNumber,
                        card.DescriptionId,
                        card.AdditionalDescription,
                        card.CurrentCondition,
                        card.ImageId,
                        card.IsActive,
                        card.src
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ProjectCheckBox>> GetGradingCondition()
        {
            await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

            await connection.OpenAsync();
            var result = await connection.QueryAsync<ProjectCheckBox>(
                sql: "[Config].[GetGradingCondition]",
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }
        public async Task<List<ProjectSubSection>> GetNormalSectionByProjectId(int projectId, int partId)
        {
            await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

            await connection.OpenAsync();
            var result = await connection.QueryAsync<ProjectSubSection>(
                sql: "[config].[GetNormalsectionsByProjectId]",
                param: new
                {
                    ProjectId = projectId,
                    PartId = partId
                },
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<List<ProjectSubSection>> GetProjectSubsectionBySectionId(Guid sectionId)
        {
            var result = await Connection.QueryAsync<ProjectSubSection>(
                sql: "[Config].[GetProjectSubsectionBySectionId]",
                param: new
                {
                    SectionId = sectionId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }

        public async Task<List<ProjectCheckBox>> GetGradingConditionByProjectId(int projectId)
        {
            var result = await Connection.QueryAsync<ProjectCheckBox>(
                sql: "[config].[GetGradigConditionByProjectId]",
                param: new
                {
                    ProjectId = projectId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }
        public async Task<List<ProjectGradingUI>> GetGradingBySectionId(int projectId, Guid sectionId)
        {
            var result = await Connection.QueryAsync<ProjectGradingUI>(
                sql: "[config].[GetProjectSectionGrading]",
                param: new
                {
                    ProjectId = projectId,
                    SectionId = sectionId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }

        public async Task<List<ImageDescriptionUI>> GetImageDescriptionsBySectionId(int projectId, Guid sectionId)
        {
            var result = await Connection.QueryAsync<ImageDescriptionUI>(
                sql: "[config].[GetProjectSectionDescription]",
                param: new
                {
                    ProjectId = projectId,
                    SectionId = sectionId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }
        public async Task<List<ImageDescriptionUI>> GetImageDescriptionsBySubSectionId(int projectId, Guid sectionId)
        {
            var result = await Connection.QueryAsync<ImageDescriptionUI>(
                sql: "[config].[GetProjectSubSectionDescription]",
                param: new
                {
                    ProjectId = projectId,
                    SectionId = sectionId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }

        public async Task<List<GenericImageCard>> GetGenericImageCard(int projectId, int templateId, Guid sectionId)
        {
            var result = await Connection.QueryAsync<GenericImageCard>(
                sql: "[config].[GetGenericImageCard]",
                param: new
                {
                    ProjectId = projectId,
                    TemplateId = templateId,
                    SectionId = sectionId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }

        public async Task<List<ProjectTankUI>> GetTankSectionByProjectId(int projectId, int partId, int imoNumber)
        {
            var result = await Connection.QueryAsync<ProjectTankUI>(
                sql: "[config].[GetTankSectionsByProjectid]",
                param: new
                {
                    ProjectId = projectId,
                    PartId = partId,
                    ImoNumber = imoNumber
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }

        public async Task<List<Core.Models.Survey.Grading>> GetH_IGrading(bool isGasCarrier)
        {
            var result = await Connection.QueryAsync<Core.Models.Survey.Grading>(
                sql: "[Config].[GetH_IGrading]",
                param: new
                {
                    GasGrading = isGasCarrier
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }

        public async Task<List<ImageDescriptionUI>> GetImageDescriptionsByTankTypeId(int projectId, int tankTypeId)
        {
            await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

            await connection.OpenAsync();
            var result = await connection.QueryAsync<ImageDescriptionUI>(
                sql: "[config].[GetImageDescriptionsByTankTypeId]",
                param: new
                {
                    ProjectId = projectId,
                    TankTypeId = tankTypeId
                },
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<List<ProjectGradingConditionMapping>> GetHandIGradingConditionByProjectId(int projectId)
        {
            try
            {
                var result = await Connection.QueryAsync<ProjectGradingConditionMapping>(
                   sql: "[config].[GetHandIProjectGradingConditionByProjectId]",
                    new { ProjectId = projectId },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                return result.ToList();
            }
            catch (SqlException)
            {
                throw; // or log
            }
        }


        public async Task<List<ProjectGradingMapping>> GetH_IProjectGradingByProjectId(int projectId)
        {
            var result = await Connection.QueryAsync<ProjectGradingMapping>(
                sql: "[config].[GetHandIProjectGradingByProjectId]",
                param: new
                {
                    ProjectId = projectId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }

        public async Task<ProjectPart> GetProjectTemplateTitle(int partId, int projectId)
        {
            try
            {
                await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

                await connection.OpenAsync();
                using (var multi = await connection.QueryMultipleAsync(
                    sql: "[config].[GetTemplateTitle]",
                    param: new
                    {
                        partId = partId,
                        projectId = projectId
                    },
                    commandType: CommandType.StoredProcedure
                ))
                {
                    return await multi.ReadFirstOrDefaultAsync<ProjectPart>();
                }
            }
            catch (Exception)
            {
                return null; // or throw / log
            }
        }
        public async Task<List<ExportPart>> GetVesselTypeExportPartConfiguration(int vesseltypeId)
        {
            DataTable dt = new DataTable();
            try
            {
                var result = await Connection.QueryAsync<ExportPart>(
                   sql: "[config].[GetVesselTypeExportConfigForParts]",
                   new
                   {
                       VesselTypeId = vesseltypeId,
                   },
                   commandType: CommandType.StoredProcedure,
                   transaction: Transaction);

                return result?.ToList();
            }

            catch (SqlException sqlEx)
            {
            }
            return null;
        }

        public async Task<List<ExportSections>> GetVesselTypeExportSectionConfiguration(int partId, int vesselTypeId)
        {
            DataTable dt = new DataTable();
            try
            {

                var result = await Connection.QueryAsync<ExportSections>(
                   sql: "[config].[GetVesselTypeExportConfigForSections]",//[config].[GetVesselTypeExportConfigForSections]
                   new
                   {
                       PartId = partId,
                       VesselTypeId = vesselTypeId

                   },
                   commandType: CommandType.StoredProcedure,
                   transaction: Transaction);

                return result?.ToList();
            }

            catch (SqlException sqlEx)
            {
            }
            return null;
        }
        public async Task<List<ExportPart>> GetProjectExportPartConfiguration(int projectId)
        {
            DataTable dt = new DataTable();
            try
            {
                var result = await Connection.QueryAsync<ExportPart>(
                   sql: "config.GetProjectExportConfigForParts",// 
                   new
                   {
                       Projectid = projectId,
                   },
                   commandType: CommandType.StoredProcedure,
                   transaction: Transaction);

                return result?.ToList();

            }

            catch (SqlException sqlEx)
            {
            }
            return null;
        }

        public async Task<List<ExportSections>> GetProjectExportSectionConfiguration(int partId, int projectId)
        {
            DataTable dt = new DataTable();
            try
            {
                var result = await Connection.QueryAsync<ExportSections>(
                   sql: "config.GetProjectExportConfigForSections",
                   new
                   {
                       PartId = partId,
                       ProjectId = projectId
                   },
                   commandType: CommandType.StoredProcedure,
                   transaction: Transaction);

                return result?.ToList();
            }
            catch (SqlException sqlEx)
            {
            }
            return null;
        }

    }

}

