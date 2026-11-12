using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class Vessel : CachedData
    {
        public string ImoNumber { get; set; }
        public string VesselName { get; set; }
        public string Description { get; set; }
        public string VesselType { get; set; }
        public string BusinessStatus { get; set; }
        public string ClassNumber { get; set; }

        public string CallSign { get; set; }
        public string OfficialNumber { get; set; }
        public string PortRegistry { get; set; }
        public PrimaryBuilder PrimaryBuilder { get; set; }
        public RegisteredOwner RegisteredOwner { get; set; }
        public string FlagName { get; set; }
        public string ClassNotation { get; set; }
        public string LifeCycleStatus { get; set; }

        public List<ServiceStats> ServiceStates { get; set; }
        public string OtherClassSociety { get; set; }
        public string DeliveryDate { get; set; }
        public string KeelLayingDate { get; set; }
        public Measurement GrossTonnage { get; set; }
        public Measurement NetTonnage { get; set; }
        public Measurement DesignDeadWeight { get; set; }
        public Measurement LengthOverall { get; set; }
        public Measurement BreadthMolded { get; set; }
        public Measurement DepthMolded { get; set; }
        public int SelectedPrefix { get; set; }


    }

    public class PrimaryBuilder
    {
        public string Shipyard { get; set; }
        public string HullNumber { get; set; }
        public string ContractDate { get; set; }
    }

    public class Measurement
    {
        public string Value { get; set; }
        public string Unit { get; set; }
    }

    public class RegisteredOwner
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Wcn { get; set; }
    }



    public class ServiceStats
    {
        public string ServiceType { get; set; }
        public string ServiceState { get; set; }
        public string StateDate { get; set; }
    }

}
