using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class FleetDashboardCertificateOverview
    {
        public int VesselId { get; set; }
        public int VesselDwId { get; set; }
        public string VesselName { get; set; }
        public DateTime? ClassExpiryDate { get; set; }
        public string ClassTerm { get; set; }
        public DateTime? MouExpiryDate { get; set; }
        public string MouTerm { get; set; }
        public DateTime? IoppExpiryDate { get; set; }
        public string IoppTerm { get; set; }
        public DateTime? IsppExpiryDate { get; set; }
        public string IsppTerm { get; set; }
        public DateTime? IappExpiryDate { get; set; }
        public string IappTerm { get; set; }
        public DateTime? LoadlineExpiryDate { get; set; }
        public string LoadlineTerm { get; set; }
        public DateTime? BwmExpiryDate { get; set; }
        public string BwmTerm { get; set; }
        public DateTime? IsmExpiryDate { get; set; }
        public string IsmTerm { get; set; }
        public DateTime? IspsExpiryDate { get; set; }
        public string IspsTerm { get; set; }
        public DateTime? AfsExpiryDate { get; set; }
        public string AfsTerm { get; set; }
        public int fleetId { get; set; }

    }
}
