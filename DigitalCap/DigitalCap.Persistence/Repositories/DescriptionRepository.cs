using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.Text;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DigitalCap.Persistence.Repositories
{
    public class DescriptionRepository : IDescriptionRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;

        public DescriptionRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public void Commit()
        {
            _unitOfWork?.Commit();
        }
        public async Task<bool> CreateProjectImageDescription(int projectId, string vesseltype)
        {
            try
            {
                await Connection.ExecuteAsync(
                            sql: "CreateProjectSectionImageDescriptions",
                             param: new
                             {
                                 ProjectId = projectId,
                                 VesselType = vesseltype
                             },
                            transaction: Transaction,
                            commandType: CommandType.StoredProcedure
                        );
                this.Commit();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<ImageDescriptions>> GetImageDescriptionsByProjectId(int projectId)
        {
            try
            {
                await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

                await connection.OpenAsync();
                var result = await connection.QueryAsync<ImageDescriptions>(
                sql: "dbo.GetImageDescriptionsByProjectId",
                param: new { ProjectId = projectId },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

                return result.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> CreateImageDescription(ImageDescriptions description)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@Description", description.Description);
            parameters.Add("@IsActive", description.IsActive);
            parameters.Add("@IsDeleted", description.IsDeleted);
            parameters.Add("@VesselType", description.VesselType);
            parameters.Add("@SectionId", description.SectionId);
            parameters.Add("@ProjectId", description.ProjectId);
            parameters.Add("@TankTypeId", description.TankTypeId);
            parameters.Add("@CategoryId", description.CategoryId);
            parameters.Add("@CreatedDttm", description.CreatedDttm);
            parameters.Add("@UpdatedDttm", description.UpdatedDttm);


            var rows = await Connection.ExecuteAsync(
                    sql: "dbo.CreateImageDescription",
                param: parameters,
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure);
            this.Commit();
            return rows > 0;
        }

        public async Task<bool> UpdateImageDescription(ImageDescriptions model)
        {
            try
            {
                await Connection.ExecuteAsync(
                    sql: "dbo.UpdateImageDescription",
                    param: new
                    {
                        model.Id,
                        model.IsActive,
                        model.IsDeleted,
                        model.Description,
                        model.SectionId,
                        model.TankTypeId,
                        CreatedDttm = DateTime.Now,
                        UpdatedDttm = DateTime.Now,
                        model.ProjectId,
                        model.CategoryId,
                        model.VesselType
                    },
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure);

                _unitOfWork.Commit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<ImageDescriptions>> CheckImageDescriptionExistsOrNot(string vesselType,string sectionName,string templateName,string description)
        {
            return await Connection.QueryAsync<ImageDescriptions>(
                "Config.CheckImageDescriptionExistsOrNot",
                new
                {
                    VesselType = vesselType,
                    SectionName = sectionName,
                    PartName = templateName,
                    Description = description?.Trim()
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );
        }

        public async Task<PagedResult<ImageDescriptions>> GetAllDescription(int pageNumber, int pageSize)
        {
            try
            {
                var multi = await Connection.QueryMultipleAsync(
                            "dbo.GetAllDescriptions",
                            new
                            {
                                PageNumber = pageNumber,
                                PageSize = pageSize
                            },
                transaction: Transaction,
                commandType: CommandType.StoredProcedure

                );

                var data = multi.Read<ImageDescriptions>().ToList();
                var total = multi.ReadFirst<int>();

                return new PagedResult<ImageDescriptions>
                {
                    Data = data,
                    Total = total
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<List<GradingSection>> GetDescriptionSectionNamesByTemplateNameAndVesselType(string templateName, string vesselType)
        {
            var result = await Connection.QueryAsync<GradingSection>(
            sql: "dbo.GetDescriptionSectionsByTemplateNameAndVesselType",
            param: new
            {
                TemplateName = templateName,
                VesselType = vesselType
            },
            transaction: Transaction,
            commandType: CommandType.StoredProcedure);

            return result.ToList();
        }


        public async Task<List<GradingTemplate>> GetGradingTemplates()
        {
            var result = await Connection.QueryAsync<GradingTemplate>(
            sql: "dbo.GetGradingTemplate",
            transaction: Transaction,
            commandType: CommandType.StoredProcedure);

            return result.ToList();
        }


        public async Task<List<GradingSection>> GetGradingSections(int templateId, string vesselType)
        {
            var result = await Connection.QueryAsync<GradingSection>(
            sql: "dbo.GetGradingSections",
            param: new
            {
                TemplateId = templateId,
                VesselType = vesselType
            },
            transaction: Transaction,
            commandType: CommandType.StoredProcedure);

            return result.ToList();
        }


        public async Task<ImageDescriptions> GetImageDescriptionById(int id)
        {
            var result = await Connection.QueryAsync<ImageDescriptions>(
            sql: "dbo.GetImageDescriptionById",
            param: new { Id = id },
            commandType: CommandType.StoredProcedure,
            transaction: Transaction);

            return result.FirstOrDefault();
        }


        public async Task<IEnumerable<GradingSection>>GetSectionNamesByTemplateAndVesselAsync(string templateName, string vesselType)
        {


            var result = await Connection.QueryAsync<GradingSection>(
            sql: "GetDescriptionSectionsByTemplateNameAndVesselType",
            param: new
            {
                TemplateName = templateName,
                VesselType = vesselType
            },
            commandType: CommandType.StoredProcedure);

            return result;
        }

    }
}
