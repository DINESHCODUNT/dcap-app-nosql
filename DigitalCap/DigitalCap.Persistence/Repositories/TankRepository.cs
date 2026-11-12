using Dapper;
using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.Models.VesselModel;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class TankRepository : RepositoryBase<VesselTank, Guid>, ITankRepository
    {
        private readonly ILogger<TankRepository> _logger;
        public TankRepository(IUnitOfWork unitOfWork, ILogger<TankRepository> logger) : base(unitOfWork, logger)
        {
            _logger = logger;
        }

        public async Task<List<VesselTank>> GetTanks_VesselByIMO(string imonumber)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ImoNumber", imonumber);

                return (await Connection.QueryAsync<VesselTank>(
                    sql: "dbo.Get_Vessel_Tank_ByIMONumber",
                    param: parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction)).ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<VesselTank>> CreateTanks_VesselByProject(string imonumber, int projectId, int newprojectId, string newimo)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ImoNumber", imonumber);
                parameters.Add("@ProjectId", projectId);
                parameters.Add("@newImoNumber", newimo);
                parameters.Add("@newProjectId", newprojectId);

                return (await Connection.QueryAsync<VesselTank>(
                    sql: "dbo.CreateTanksofVesselByProject",
                    param: parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction)).ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> PopulateTemplate(string imonumber = null, string vesseltype = null, string copyingimonumber = null)
        {
            try
            {
                var tanks = new List<VesselTank>();
                var gradings = new List<VesselTankGrading>();


                if (!string.IsNullOrEmpty(copyingimonumber))
                {
                    tanks = this.GetTanks_VesselByIMO(copyingimonumber).Result;


                    foreach (var tank in tanks)
                    {
                        tank.Id = Guid.NewGuid();
                        tank.ImoNumber = imonumber;
                        tank.CreatedDttm = DateTime.Now;
                        tank.UpdateDttm = DateTime.Now;
                        var result = this.CreateTank(tank).Result;
                    }

                }

                if (!string.IsNullOrEmpty(vesseltype))
                {
                    tanks = this.GetTanks_VesselByVesselType(vesseltype).Result;


                    foreach (var tank in tanks)
                    {
                        tank.Id = Guid.NewGuid();
                        tank.ImoNumber = imonumber;
                        tank.CreatedDttm = DateTime.Now;
                        tank.VesselType = null;
                        tank.UpdateDttm = DateTime.Now;
                        var result = this.CreateTank(tank).Result;
                    }

                }


                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> PopulateTemplate(string imonumber = null, string vesseltype = null, string copyingimonumber = null, int projectid = 0, int copyingProjectId = 0)
        {
            try
            {
                var tanks = new List<VesselTank>();
                if (!string.IsNullOrEmpty(copyingimonumber))
                {
                    tanks = copyingProjectId == 0 ? GetTanks_VesselByIMO(copyingimonumber).Result : CreateTanks_VesselByProject(copyingimonumber, copyingProjectId, projectid, imonumber).Result;

                }
                if (!string.IsNullOrEmpty(vesseltype))
                {
                    tanks = this.GetTanks_VesselByVesselType(vesseltype).Result;
                    foreach (var tank in tanks)
                    {
                        tank.Id = Guid.NewGuid();
                        tank.ImoNumber = imonumber;
                        tank.CreatedDttm = DateTime.Now;
                        tank.VesselType = null;
                        tank.UpdateDttm = DateTime.Now;
                        tank.ProjectId = projectid;
                        var result = this.CreateTank(tank).Result;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<List<VesselTank>> GetTanks_VesselByVesselType(string vesseltype)
        {

            try
            {
                return (await Connection.QueryAsync<VesselTank>(
                    sql: "dbo.Get_Vessel_Tank_ByVesselType",
                    param: new
                    {
                        VesselType = vesseltype
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction)).ToList();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> CreateTank(VesselTank vesselTank)
        {
            try
            {
                var parameters = new DynamicParameters();

                parameters.Add("@Id", vesselTank.Id);
                parameters.Add("@VesselType", vesselTank.VesselType);
                parameters.Add("@TankName", vesselTank.TankName);
                parameters.Add("@Subheader", vesselTank.Subheader);
                parameters.Add("@TankTypeId", vesselTank.TankTypeId);
                parameters.Add("@ImoNumber", vesselTank.ImoNumber);
                parameters.Add("@TankTypeId", vesselTank.TankTypeId);
                parameters.Add("@TankName", vesselTank.TankName);
                parameters.Add("@IsActive", vesselTank.IsActive);
                parameters.Add("@IsDeleted", vesselTank.IsDeleted);
                parameters.Add("@CreatedDttm", vesselTank.CreatedDttm);
                parameters.Add("@UpdateDttm", vesselTank.UpdateDttm);
                parameters.Add("@IsDeleted", vesselTank.IsDeleted);
                parameters.Add("@Subheader", vesselTank.Subheader);
                parameters.Add("@RequiredInReport", vesselTank.RequiredInReport);
                parameters.Add("@ProjectId", vesselTank.ProjectId);

                await Connection.ExecuteAsync(
                            sql: "dbo.Create_Vessel_TankMapping",
                            param: parameters,
                            transaction: Transaction,
                            commandType: CommandType.StoredProcedure
                        );

                this.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vessel tank");
                return false;
            }
        }

        public async Task<List<Core.Models.Tank.ShipType>> GetShipType()
        {
            try
            {
                var result = await Connection.QueryAsync<Core.Models.Tank.ShipType>(
                    sql: "dbo.GetShipType",
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                return result.ToList();

            }
            catch (Exception)
            {
                throw; // API layer can handle error
            }
        }

        public async Task<List<VesselTank>> GetTanks_VesselByProject(string imoNumber, int projectId)
        {
            try
            {
                var result = await Connection.QueryAsync<VesselTank>(
                      sql: "[dbo].[Get_Vessel_Tank_ByProject]",
                      param: new
                      {
                          ImoNumber = imoNumber,
                          ProjectId = projectId
                      },
                       transaction: Transaction,
                       commandType: CommandType.StoredProcedure
                  );
                return result.ToList();

            }
            catch
            {
                throw;
            }
        }
        public async Task<List<VesselTankGrading>> GetVessel_GradingByVesselType(string vesseltype)
        {
            try
            {
                var result = await Connection.QueryAsync<VesselTankGrading>(
                       sql: "dbo.Get_Vessel_Tank_Grading_ByVesselType",
                      param: new
                      {

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
                return false;
            }
        }
        public async Task<List<TankUI>> GetTemplateTanks(int templateId, string imonumber, string vesseltype, int projectId)
        {
            try
            {

                var result = await Connection.QueryAsync<TankUI>(
                    sql: "dbo.GetTempaleTanks",
                    param: new
                    {
                        TemplateId = templateId,
                        ImoNumber = imonumber,
                        VesselType = vesseltype,
                        ProjectId = projectId
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction);

                return result.ToList();

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<TankCheckBox>> GetTemplateTankGradingCondition(int Id)
        {
            try
            {
                await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

                await connection.OpenAsync();
                var result = await connection.QueryAsync<TankCheckBox>(
                    sql: "dbo.GetTankGradingConditionById",
                      new
                      {
                          Id = Id
                      },
                    commandType: CommandType.StoredProcedure);

                return result.ToList();

            }
            catch (Exception ex)
            {
                return new List<TankCheckBox>();
            }
        }

        public async Task<List<TankGradingUI>> GetTemplateTankGrading(int tanktypeId, int projectId, string vesseltype)
        {
            try
            {
                var result = await Connection.QueryAsync<TankGradingUI>(
                        sql: "dbo.GetTempaleTankGrading",
                          new
                          {
                              TankTypeId = tanktypeId,
                              ProjectId = projectId,
                              VesselType = vesseltype
                          },
                        commandType: CommandType.StoredProcedure,
                        transaction: Transaction);

                return result.ToList();

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<TankImageCard>> GetProjectTankImageCard(int projectId, int templateId, Guid sectionId)
        {
            await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

            await connection.OpenAsync();
            var result = await connection.QueryAsync<TankImageCard>(
                        sql: "dbo.GetTankImageCard",
                          new
                          {
                              ProjectId = projectId,
                              TemplateId = templateId,
                              SectionId = sectionId
                          },
                        commandType: CommandType.StoredProcedure);

            return result.ToList();

        }

        public async Task<TankImageCard> GetProjectTankImagCardByName(int projectId, int templateId, Guid sectionId, int cardNumber)
        {
            try
            {
                var result = await Connection.QueryFirstOrDefaultAsync<TankImageCard>(
                    sql: "dbo.GetProjectTankImageCardByName",
                    param: new
                    {
                        ProjectId = projectId,
                        TemplateId = templateId,
                        SectionId = sectionId,
                        CardNumber = cardNumber
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                return result;
            }
            catch (Exception)
            {
                return null; // or throw/log based on your standard
            }
        }
        public async Task<bool> UpdateProjectTankImageCard(TankImageCard tankImageCard)
        {
            var affectedRows = await Connection.ExecuteAsync(
                sql: "dbo.UpdateTankImageCard",
                param: new
                {
                    tankImageCard.ProjectId,
                    tankImageCard.TemplateId,
                    tankImageCard.SectionId,
                    tankImageCard.CardNumber,
                    tankImageCard.CardName,
                    tankImageCard.DescriptionId,
                    tankImageCard.AdditionalDescription,
                    tankImageCard.CurrentCondition,
                    tankImageCard.IsActive
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return affectedRows > 0;
        }
        public async Task<bool> CreateProjectTankImageCard(TankImageCard tankImageCard)
        {
            var affectedRows = await Connection.ExecuteAsync(
                sql: "dbo.CreateTankImageCard",
                param: new
                {
                    tankImageCard.ProjectId,
                    tankImageCard.TemplateId,
                    tankImageCard.SectionId,
                    tankImageCard.CardNumber,
                    tankImageCard.CardName,
                    tankImageCard.DescriptionId,
                    tankImageCard.AdditionalDescription,
                    tankImageCard.CurrentCondition,
                    tankImageCard.IsActive,
                    tankImageCard.ImageId,
                    // ❌ Id, src, IsSync intentionally excluded
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return affectedRows > 0;
        }
        public async Task<List<TankTypes>> GetTankTypes()
        {
            try
            {
                var result = await Connection.QueryAsync<TankTypes>(
                 sql: "dbo.GetTankType",
                 commandType: CommandType.StoredProcedure,
                 transaction: Transaction);

                return result.ToList();

            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public async Task<List<TankTypes>> GetTankTypes(int projectId, string vesseltype)
        {
            try
            {
                var result = await Connection.QueryAsync<TankTypes>(
                  sql: "[dbo].[GetTankTypeByTankMapping]",
                  new
                  {
                      ProjectId = projectId,
                      Veseltype = vesseltype
                  },
                 commandType: CommandType.StoredProcedure,
                 transaction: Transaction);
                return result.ToList();

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<VesselDetails>> GetVesselIMONo()
        {
            try
            {
                var result = await Connection.QueryAsync<VesselDetails>(
                sql: "dbo.GetIMONumber",
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);

                return result.ToList();

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<Core.Models.View.Admin.VesselTypes>> GetVesselType()
        {
            try
            {
                var result = await Connection.QueryAsync<Core.Models.View.Admin.VesselTypes>(
                    sql: "dbo.GetVesselType",
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction);

                return result.ToList();

            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public async Task<List<Core.Models.View.Admin.Tank>> GetTanks_Vessel(string username = null)
        {
            try
            {
                var result = await Connection.QueryAsync<Core.Models.View.Admin.Tank>(
                    sql: "dbo.Get_Vessel_Tank",
                    param: new
                    {
                        username = username
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300, // 5 minutes
                    transaction: Transaction
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<VesselTank> GetTanks_VesselById(Guid? Id, int? Projectid)
        {
            try
            {
                var result = await Connection.QueryAsync<VesselTank>(
                    sql: "dbo.Get_Vessel_Tank_ById",
                      new
                      {
                          Id = Id,
                          ProjectId = Projectid
                      },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction);

                return result.ToList().FirstOrDefault();

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> UpdateTank(VesselTank vesselTank)
        {
            try
            {
                var rowsAffected = await Connection.ExecuteAsync(
                    "dbo.Update_Vessel_TankMapping",
                    new
                    {
                        Id = vesselTank.Id,
                        VesselType = vesselTank.VesselType,
                        ImoNumber = vesselTank.ImoNumber,
                        TankTypeId = vesselTank.TankTypeId,
                        TankName = vesselTank.TankName,
                        IsActive = vesselTank.IsActive,
                        UpdateDttm = vesselTank.UpdateDttm,
                        IsDeleted = vesselTank.IsDeleted,
                        Subheader = vesselTank.Subheader,
                        RequiredInReport = vesselTank.RequiredInReport,
                        ProjectId = vesselTank.ProjectId
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                    );

                this.Commit();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vessel tank"); // optional but recommended
                return false;
            }
        }


        public async Task<List<VesselTankDetails>> GetVesselTypeList()
        {
            try
            {
                var result = await Connection.QueryAsync<VesselTankDetails>(
                    sql: "dbo.GetVesselTypeList",
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction);

                return result.ToList();

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<IMOTankFilterModel>> GetProjectNames(string imoNumber)
        {
            try
            {
                var result = await Connection.QueryAsync<IMOTankFilterModel>(
                    sql: "CAP.GET_PROJECTNAME_BYIMO",
                    new { IMO = imoNumber },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction);
                return result.ToList();

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> UpdateStatus(List<Guid> tankIds, bool status)
        {
            if (tankIds == null || tankIds.Count == 0)
                return false;

            try
            {
                // Prepare TVP DataTable
                var tankIdTable = new DataTable();
                tankIdTable.Columns.Add("TankId", typeof(Guid));

                foreach (var id in tankIds)
                {
                    tankIdTable.Rows.Add(id);
                }

                var parameters = new DynamicParameters();
                parameters.Add(
                    "@TankIds",
                    tankIdTable.AsTableValuedParameter("dbo.TankIdTableType")
                );
                parameters.Add("@Status", status);

                var affectedRows = await Connection.ExecuteAsync(
                    sql: "dbo.UpdateTankStatus",
                    param: parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                Commit(); // commit only after successful execution
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                // TODO: log ex
                return false;
            }
        }

        public Task<IEnumerable<Core.Enumerations.ShipType>> GetShipTypes()
        {
            throw new NotImplementedException();
        }
    }
}
