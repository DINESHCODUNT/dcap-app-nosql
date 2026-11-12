using DigitalCap.Core.Models.VesselModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IFreedomAPIRepository
    {
        Task<Vessel> GetVessel(string classNumber);
        Task<IEnumerable<VesselSurvey>> GetSurveys(string classNum);
        Task<IEnumerable<Certificate>> GetCertificates(string classNum);
    }
}
