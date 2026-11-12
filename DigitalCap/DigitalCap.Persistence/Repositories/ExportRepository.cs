using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.Models.ReportConfig;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class ExportRepository : IExportRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        protected IDbConnection Connection => _unitOfWork.Connection;
        protected IDbTransaction Transaction => _unitOfWork.Transaction;

        public ExportRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public void Commit()
        {
            _unitOfWork?.Commit();
        }

        public async Task<List<ExportPart>> GetVesselTypeExportPartConfiguration(int vesselTypeId)
        {
            try
            {
                return (await Connection.QueryAsync<ExportPart>(
                    sql: "[config].[GetVesselTypeExportConfigForParts]",
                    param: new
                    {
                        VesselTypeId = vesselTypeId
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                )).ToList();
            }
            catch (Exception ex)
            {
                // Optional: log ex
                return null;
            }
        }

        public async Task<List<ExportSections>> GetVesselTypeExportSectionConfiguration(int partId, int vesselTypeId)
        {
            try
            {
                return (await Connection.QueryAsync<ExportSections>(
                    sql: "[config].[GetVesselTypeExportConfigForSections]",
                    param: new
                    {
                        PartId = partId,
                        VesselTypeId = vesselTypeId
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                )).ToList();
            }
            catch (Exception ex)
            {
                // Optional: log exception
                return null;
            }
        }

        public async Task<List<ExportPart>> GetProjectExportPartConfiguration(int projectId)
        {
            try
            {
                return (await Connection.QueryAsync<ExportPart>(
                    sql: "config.GetProjectExportConfigForParts",
                    param: new
                    {
                        ProjectId = projectId
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                )).ToList();
            }
            catch (Exception ex)
            {
                // Optional: log ex
                return null;
            }
        }

        public async Task<List<ExportSections>> GetProjectExportSectionConfiguration(int partId, int projectId)
        {
            try
            {
                return (await Connection.QueryAsync<ExportSections>(
                    sql: "config.GetProjectExportConfigForSections",
                    param: new
                    {
                        PartId = partId,
                        ProjectId = projectId
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                )).ToList();
            }
            catch (Exception ex)
            {
                // Optional: log exception
                return null;
            }
        }

        public async Task<bool> UpdatePartRequiredInReportAsync(bool requiredInReport, int partId, int projectId, int vesselTypeId, string updatedBy)
        {
            try
            {
                await Connection.ExecuteAsync(
                    sql: "config.UpdatePartRequiredInReport",
                    param: new
                    {
                        RequiredInReport = requiredInReport,
                        PartId = partId,
                        ProjectId = projectId,
                        UpdatedBy = updatedBy,
                        VesselTypeId = vesselTypeId
                    },
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure
                );
                this.Commit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        

        public async Task<bool> UpdateTankSectionRequiredInReportAsync(bool requiredInReport, Guid sectionId, int projectId, string updatedBy)
        {
            try
            {
                await Connection.ExecuteAsync(
                    sql: "config.UpdateTankSectionRequiredInReport",
                    param: new
                    {
                        RequiredInReport = requiredInReport,
                        SectionId = sectionId,
                        ProjectId = projectId,
                        UpdatedBy = updatedBy
                    },
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure
                );

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public async Task<bool> UpdateSectionRequiredInReportAsync(bool requiredInReport, Guid sectionId, int projectId, int vesselTypeId, string updatedBy)
        {
            try
            {
                await Connection.ExecuteAsync(
                    sql: "config.UpdateSectionRequiredInReport",
                    param: new
                    {
                        RequiredInReport = requiredInReport,
                        SectionId = sectionId,
                        ProjectId = projectId,
                        VesselTypeId = vesselTypeId,
                        UpdatedBy = updatedBy
                    },
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure
                );
                this.Commit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
