using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.Models.VesselModel;
using DigitalCap.Core.Models.View.Admin;
using System;
using System.Collections.Generic;
using System.Text;
using ShipType = DigitalCap.Core.Models.Tank.ShipType;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface ITankRepository : IRepositoryBase<VesselTank, Guid>
    {
        Task<List<VesselTank>> GetTanks_VesselByIMO(string imonumber);
        Task<bool> PopulateTemplate(string imonumber = null, string vesseltype = null, string copyingimonumber = null);
        Task<bool> PopulateTemplate(string imonumber = null, string vesseltype = null, string copyingimonumber = null, int projectid = 0, int copyingProjectId = 0);
        Task<List<VesselTank>> GetTanks_VesselByVesselType(string vesseltype);
        Task<bool> CreateTank(VesselTank vesselTank);
        Task<List<ShipType>> GetShipType();
        Task<List<VesselTank>> GetTanks_VesselByProject(string imonumber, int projectId);
        Task<List<VesselTankGrading>> GetVessel_GradingByVesselType(string vesseltype);
        Task<bool> CreateVessel_Grading(VesselTankGrading vesselTankGrading);
        Task<List<TankUI>> GetTemplateTanks(int templateId, string imonumber, string vesseltype, int projectId);
        Task<List<TankCheckBox>> GetTemplateTankGradingCondition(int Id);
        Task<List<TankGradingUI>> GetTemplateTankGrading(int tanktypeId, int projectId, string vesseltype);
        Task<List<TankImageCard>> GetProjectTankImageCard(int projectId, int templateId, Guid sectionId);
        Task<TankImageCard> GetProjectTankImagCardByName(int projectId, int templateId, Guid sectionId, int cardNumber);
        Task<bool> UpdateProjectTankImageCard(TankImageCard tankImageCard);
        Task<bool> CreateProjectTankImageCard(TankImageCard tankImageCard);
        Task<List<TankTypes>> GetTankTypes();
        Task<List<TankTypes>> GetTankTypes(int projectId, string vesseltype);
        Task<List<Core.Models.View.Admin.VesselTypes>> GetVesselType();
        Task<List<VesselDetails>> GetVesselIMONo();
        Task<List<Core.Models.View.Admin.Tank>> GetTanks_Vessel(string username = null);
        Task<VesselTank> GetTanks_VesselById(Guid? Id, int? Projectid);
        Task<bool> UpdateTank(VesselTank vesselTank);
        Task<List<VesselTankDetails>> GetVesselTypeList();
        Task<List<IMOTankFilterModel>> GetProjectNames(string imoNumber);
        Task<bool> UpdateStatus(List<Guid> tankIds, bool status);
        Task<IEnumerable<Enumerations.ShipType>> GetShipTypes();
    }
}
