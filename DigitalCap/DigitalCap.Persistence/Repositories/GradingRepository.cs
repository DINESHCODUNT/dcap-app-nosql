using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
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
    public class GradingRepository : IGradingRepository
    {


        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;
        public GradingRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        public void Commit()
        {
            _unitOfWork?.Commit();
        }
        public async Task<int> CreateProjectSectionGrading(int projectId, string vesselType)
        {
            var result = await Connection.ExecuteAsync(
                sql: "[dbo].[CreateProjectSectionGrading]",
                param: new
                {
                    ProjectId = projectId,
                    VesselType = vesselType
                },

                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
            this.Commit();
            return result;
        }

        public async Task<bool> CreateVessel_Grading(VesselTankGrading vesselTankGrading)
        {
            try
            {
                var parameters = new DynamicParameters();

                foreach (var prop in typeof(VesselTankGrading).GetProperties())
                {
                    if (prop.Name != "Id" &&
                        prop.Name != "TemplateId" &&
                        prop.Name != "VesselName")
                    {
                        parameters.Add("@" + prop.Name, prop.GetValue(vesselTankGrading));
                    }
                }

                await Connection.ExecuteAsync(
                    sql: "dbo.Create_Vessel_Grading",
                    param: parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                return true;
            }
            catch (Exception ex)
            {
                // optional: log ex
                return false;
            }
        }

        public async Task<List<Grading>> GetAllGradingAsync()
        {
            var result = await Connection.QueryAsync<Grading>(
            sql: "dbo.GetAllGrading",
            commandType: CommandType.StoredProcedure,
            transaction: Transaction);

            return result.ToList();
        }

        public async Task<PagedResult<Grading>> GetAllGrading_Pagination(int pageNumber, int pageSize)
        {
            var multi = await Connection.QueryMultipleAsync(
                "dbo.GetAllGrading_Pagination",
                new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            var data = multi.Read<Grading>().ToList();
            var total = multi.ReadFirst<int>();

            return new PagedResult<Grading>
            {
                Data = data,
                Total = total
            };
        }

        public async Task<bool> CreateTankGradingAsync(Grading grading)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@VesselType", grading.VesselType);
            parameters.Add("@GradingName", grading.GradingName);
            parameters.Add("@IsActive", grading.IsActive);
            parameters.Add("@TankTypeId", grading.TanktypeId);
            parameters.Add("@ProjectId", grading.ProjectId);
            parameters.Add("@RequiredInReport", grading.RequiredInReport);

            var rows = await Connection.ExecuteAsync(
                sql: "dbo.CreateTankGrading",
                param: parameters,
                transaction: Transaction,
                commandType: CommandType.StoredProcedure);
            return rows > 0;
        }

        public async Task<bool> CreateSectionGradingAsync(Grading grading)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@LabelName", grading.GradingName);
            parameters.Add("@IsActive", grading.IsActive);
            parameters.Add("@IsDeleted", grading.IsDeleted);
            parameters.Add("@VesselType", grading.VesselType);
            parameters.Add("@SectionId", grading.SectionId);
            parameters.Add("@ProjectId", grading.ProjectId);
            parameters.Add("@RequiredInReport", grading.RequiredInReport);

            var rows = await Connection.ExecuteAsync(
                sql: "dbo.CreateSectionGrading",
                param: parameters,
                transaction: Transaction,
                commandType: CommandType.StoredProcedure);
            this.Commit();
            return rows > 0;
        }

        public async Task<bool> UpdateTankGradingAsync(Grading grading)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@GradingId", grading.GradingId);
            parameters.Add("@VesselType", grading.VesselType);
            parameters.Add("@GradingName", grading.GradingName);
            parameters.Add("@IsActive", grading.IsActive);
            parameters.Add("@TankTypeId", grading.TanktypeId);
            parameters.Add("@ProjectId", grading.ProjectId);
            parameters.Add("@RequiredInReport", grading.RequiredInReport);

            var rows = await Connection.ExecuteAsync(
                sql: "dbo.UpdateTankGrading",
                param: parameters,
                transaction: Transaction,
                commandType: CommandType.StoredProcedure);
            this.Commit();
            return rows > 0;
        }

        public async Task<bool> UpdateSectionGradingAsync(Grading grading)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@GradingId", grading.GradingId);
            parameters.Add("@LabelName", grading.GradingName);
            parameters.Add("@IsActive", grading.IsActive);
            parameters.Add("@VesselType", grading.VesselType);
            parameters.Add("@SectionId", grading.SectionId);
            parameters.Add("@IsDeleted", grading.IsDeleted);
            parameters.Add("@ProjectId", grading.ProjectId);
            parameters.Add("@RequiredInReport", grading.RequiredInReport);

            var rows = await Connection.ExecuteAsync(
                sql: "dbo.UpdateSectionGrading",
                param: parameters,
                transaction: Transaction,
                commandType: CommandType.StoredProcedure);
            this.Commit();
            return rows > 0;
        }

        public async Task<List<GradingTemplate>> GetGradingTemplates()
        {
            try
            {
                var result = await Connection.QueryAsync<GradingTemplate>(
                sql: "dbo.GetGradingTemplate",
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
          );

                return result.ToList();
            }
            catch (Exception ex)
            {
                // optional: log exception
                return null;
            }
        }

        public async Task<bool> DeleteGradingAsync(int gradingId, int tankId)
        {
            var rows = await Connection.ExecuteAsync(
                sql: "dbo.DeleteGrading",
                param: new { GradingId = gradingId, TankId = tankId },
                transaction: Transaction,
                commandType: CommandType.StoredProcedure);
            this.Commit();
            return rows > 0;
        }

        public async Task<List<GradingTemplate>> GetGradingTemplatesByVesselType(string Vesseltype)
        {
            try
            {
                var result = await Connection.QueryAsync<GradingTemplate>(
                    sql: "dbo.GetGradingTemplate",
                    new { VesselType = Vesseltype },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction);

                return result.ToList();

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<GradingSection>> GetGradingSectionNamesByTemplateNameAndVesselType(string templateName, string vesseltype)
        {
            try
            {
                var result = await Connection.QueryAsync<Core.Models.Grading.GradingSection>(
                       sql: "dbo.GetGradingSectionsByTemplateNameAndVesselType",
                        new
                        {
                            TemplateName = templateName,
                            VesselType = vesseltype

                        },
                       commandType: CommandType.StoredProcedure, transaction: Transaction);

                return result.ToList();

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<GradingSection>> GetGradingSections(int templateId, string vesseltype)
        {
            try
            {
                var result = await Connection.QueryAsync<Core.Models.Grading.GradingSection>(
                    sql: "dbo.GetGradingSections",
                     new
                     {
                         TemplateId = templateId,
                         VesselType = vesseltype
                     },
                    commandType: CommandType.StoredProcedure, transaction: Transaction);

                return result.ToList();
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }

}

