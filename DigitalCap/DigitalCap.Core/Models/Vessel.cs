using DigitalCap.Core.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class Vessel
    {
        [Key]
        public int Vessel_ID { get; set; }
        public string Source { get; set; }
        public double Beam { get; set; }
        public double Breadth { get; set; }
        public DateTime ContractDate { get; set; }
        public double Depth { get; set; }
        public double Draft { get; set; }


        [Display(Name = "Hull Number")]
        public string HullNum { get; set; }
        [Display(Name = "IMO Number")]
        public int IMONum { get; set; }
        public double Length { get; set; }
        [Display(Name = "Market Segment")]
        public string MarketSegment { get; set; }
        [Display(Name = "Vessel Name")]
        public string VesselName { get; set; }
        [Display(Name = "Market SubSegment")]
        public string MarketSubSegment { get; set; }
        [Display(Name = "Call Sign")]
        public string CallSign { get; set; }
        public double? NetTonnage { get; set; }
        public string ClassNotation { get; set; }

        public string ShipTypeGrouping { get; set; }
        public string Statcode5 { get; set; }
        public int Liquid { get; set; }
        public string RegisteredOwner { get; set; }
        public string ProjectName { get; set; }
        public string Manager { get; set; }
        public string Operator { get; set; }
        public string Technical_Manager { get; set; }

        public int Registered_Owner_ID { get; set; }
        public int Manager_ID { get; set; }
        public int Operator_ID { get; set; }
        public int Technical_Manager_ID { get; set; }
        public DateTime? BuildDate { get; set; }
        public string Builder { get; set; }
        public string BuilderName { get; set; }
        public int BuilderNum { get; set; }
        public string ClassNum { get; set; }
        public DateTime ClassedDate { get; set; }
        public string Country { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string FlagName { get; set; }
        public DateTime KeelLayingDate { get; set; }
        public DateTime LaunchingDate { get; set; }
        public string OwnerCountry { get; set; }
        public int OwnerID { get; set; }
        public string OwnerName { get; set; }
        public string VesselDescription { get; set; }
        public string CommercialCategory { get; set; }
        public string ClassedBy { get; set; }
        public string ClassedBy2 { get; set; }
        public string SubStatus { get; set; }
        public double? DWT { get; set; }
        public double? GT { get; set; }
        public string VesselType { get; set; }
        /// <summary>
        /// Helper to dto a vessel
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public static Vessel LookUpDTO(DataRow dr)
        {
            Vessel v = new Vessel();
            try
            {
                if (dr[VesselColumnNames.DeliveryDate] != DBNull.Value)
                {
                    v.BuildDate = Convert.ToDateTime(dr[VesselColumnNames.DeliveryDate]);
                }

                v.Builder = dr[VesselColumnNames.BuilderName].ToString();
                v.ClassedBy = dr[VesselColumnNames.ClassedBy].ToString();
                v.ClassedBy2 = dr[VesselColumnNames.ClassedBy2].ToString();

                if (dr[VesselColumnNames.ClassNum] != DBNull.Value && !string.IsNullOrEmpty(dr[VesselColumnNames.ClassNum].ToString()))
                {
                    v.ClassNum = dr[VesselColumnNames.ClassNum].ToString();
                }

                v.HullNum = dr[VesselColumnNames.HullNum].ToString();
                v.Length = dr[VesselColumnNames.Length] is DBNull
                            ? 0
                            : Convert.ToDouble(dr[VesselColumnNames.Length]);

                v.VesselName = dr[VesselColumnNames.VesselName].ToString();
                v.VesselType = Convert.ToString(dr[VesselColumnNames.VesselType]);
            }
            catch (Exception ex)
            {
                v.VesselDescription = ex.Message; //TODO
            }
            return v;
        }



        public static Vessel MapVessel(DigitalCap.Core.Models.VesselModel.Vessel vesselDetails)
        {
            Vessel vesselTable = new Vessel();



            vesselTable.VesselName = vesselDetails.VesselName;
            vesselTable.IMONum = Convert.ToInt32(vesselDetails.ImoNumber);
            if (vesselDetails.PrimaryBuilder != null)
            {
                vesselTable.Builder = vesselDetails.PrimaryBuilder.Shipyard;
                vesselTable.HullNum = vesselDetails.PrimaryBuilder.HullNumber;
                vesselTable.ShipTypeGrouping = vesselDetails.PrimaryBuilder.Shipyard;
                if (!string.IsNullOrEmpty(vesselDetails.PrimaryBuilder.ContractDate))
                {
                    vesselTable.ContractDate = Convert.ToDateTime(vesselDetails.PrimaryBuilder.ContractDate);
                }


            }
            vesselTable.VesselType = vesselDetails.VesselType;
            if (!string.IsNullOrEmpty(vesselDetails.DeliveryDate))
            {
                vesselTable.BuildDate = Convert.ToDateTime(vesselDetails.DeliveryDate);
            }
            vesselTable.FlagName = vesselDetails.FlagName;
            vesselTable.CallSign = vesselDetails.CallSign;
            vesselTable.OwnerName = vesselDetails.RegisteredOwner.Name;
            vesselTable.Manager = null;
            vesselTable.Length = !string.IsNullOrEmpty(vesselDetails.LengthOverall.Value) ? Convert.ToDouble(vesselDetails.LengthOverall.Value) : 0;
            vesselTable.Breadth = !string.IsNullOrEmpty(vesselDetails.BreadthMolded.Value) ? Convert.ToDouble(vesselDetails.BreadthMolded.Value) : 0;
            vesselTable.Depth = !string.IsNullOrEmpty(vesselDetails.DepthMolded.Value) ? Convert.ToDouble(vesselDetails.DepthMolded.Value) : 0;
            vesselTable.NetTonnage = !string.IsNullOrEmpty(vesselDetails.NetTonnage.Value) ? Convert.ToDouble(vesselDetails.NetTonnage.Value) : 0;
            vesselTable.ClassNotation = vesselDetails.ClassNotation;
            vesselTable.Vessel_ID = Convert.ToInt32(vesselDetails.ImoNumber);
            vesselTable.ClassedBy = vesselDetails.OtherClassSociety;

            vesselTable.VesselName = vesselDetails.VesselName;
            vesselTable.CallSign = vesselDetails.CallSign;

            vesselTable.Statcode5 = null;
            vesselTable.GT = vesselDetails.GrossTonnage != null ? Convert.ToDouble(vesselDetails.GrossTonnage.Value) : null;
            vesselTable.RegisteredOwner = null;
            vesselTable.ProjectName = null;
            vesselTable.Manager = null;
            vesselTable.Operator = null;
            vesselTable.Technical_Manager = null;
            vesselTable.ClassNum = vesselDetails.ClassNumber;
            vesselTable.ClassedDate = vesselDetails.ServiceStates.Where(x => x.ServiceState == "Classed").Select(x => Convert.ToDateTime(x.StateDate)).FirstOrDefault();

            if (!string.IsNullOrEmpty(vesselDetails.DeliveryDate))
            {
                vesselTable.DeliveryDate = Convert.ToDateTime(vesselDetails.DeliveryDate);
            }
            vesselTable.FlagName = vesselDetails.FlagName;
            if (!string.IsNullOrEmpty(vesselDetails.KeelLayingDate))
            {
                vesselTable.KeelLayingDate = Convert.ToDateTime(vesselDetails.KeelLayingDate);
            }

            vesselTable.VesselDescription = vesselDetails.Description;
            vesselTable.DWT = Convert.ToDouble(vesselDetails.DesignDeadWeight.Value);


            return vesselTable;

        }


        public static Vessel DetailsDTO(DataRow dr)
        {
            Vessel v = new Vessel();
            try
            {
                v.Beam = Convert.ToDouble(dr[ColumnNames.Beam]);
                v.Breadth = Convert.ToDouble(dr[ColumnNames.Breadth]);

                if (dr[ColumnNames.BuildDate] != DBNull.Value)
                {
                    v.BuildDate = Convert.ToDateTime(dr[ColumnNames.BuildDate]);
                }

                v.Builder = dr[ColumnNames.Builder].ToString();
                v.BuilderName = dr[ColumnNames.BuilderName].ToString();
                v.BuilderNum = Convert.ToInt32(dr[ColumnNames.BuilderNum]);
                v.CallSign = dr[ColumnNames.CallSign] == DBNull.Value ? null : dr[ColumnNames.CallSign].ToString();
                v.ClassedBy = dr[ColumnNames.ClassedBy].ToString();
                v.ClassedBy2 = dr[ColumnNames.ClassedBy2].ToString();

                if (dr[ColumnNames.ClassNum] != DBNull.Value && !string.IsNullOrEmpty(dr[ColumnNames.ClassNum].ToString()))
                {
                    v.ClassNum = dr[ColumnNames.ClassNum].ToString();
                }

                v.CommercialCategory = dr[ColumnNames.CommercialCategory].ToString();
                v.Country = dr[ColumnNames.Country].ToString();
                if (dr[ColumnNames.Depth] != DBNull.Value && !string.IsNullOrEmpty(dr[ColumnNames.Depth].ToString()))
                {
                    v.Depth = Convert.ToDouble(dr[ColumnNames.Depth]);
                }
                if (dr[ColumnNames.Draft] != DBNull.Value && !string.IsNullOrEmpty(dr[ColumnNames.Draft].ToString()))
                {
                    v.Draft = Convert.ToDouble(dr[ColumnNames.Draft]);
                }
                if (dr[ColumnNames.IMONum] != DBNull.Value && !string.IsNullOrEmpty(dr[ColumnNames.IMONum].ToString()))
                {
                    v.IMONum = Convert.ToInt32(dr[ColumnNames.IMONum]); //Sometimes this is a string!
                }
                if (dr[ColumnNames.Draft] != DBNull.Value && !string.IsNullOrEmpty(dr[ColumnNames.Draft].ToString()))
                {
                    v.Draft = Convert.ToDouble(dr[ColumnNames.Draft]);
                }

                v.DWT = dr[ColumnNames.DWT] == DBNull.Value ? null : Convert.ToDouble(dr[ColumnNames.DWT]);
                v.GT = dr[ColumnNames.GT] == DBNull.Value ? 0 : Convert.ToDouble(dr[ColumnNames.GT]);
                v.FlagName = dr[ColumnNames.FlagName].ToString();
                v.HullNum = dr[ColumnNames.HullNum].ToString();

                v.Length = dr[ColumnNames.Length] is DBNull
                            ? 0
                            : Convert.ToDouble(dr[ColumnNames.Length]);
                v.MarketSegment = dr[ColumnNames.MarketSegment].ToString();

                v.NetTonnage = dr[ColumnNames.NetTonnage] == DBNull.Value ? null : Convert.ToDouble(dr[ColumnNames.NetTonnage]);
                v.Operator = dr[ColumnNames.Operator].ToString();
                v.ShipTypeGrouping = dr[ColumnNames.ShipTypeGrouping].ToString();
                v.Statcode5 = dr[ColumnNames.Statcode5].ToString();
                v.SubStatus = dr[ColumnNames.SubStatus].ToString();
                v.VesselDescription = dr[ColumnNames.VesselDescription].ToString();
                v.VesselName = dr[ColumnNames.VesselName].ToString();
                v.Vessel_ID = Convert.ToInt32(dr[ColumnNames.Vessel_ID]);
                v.VesselType = Convert.ToString(dr[ColumnNames.VesselType]);
            }
            catch (Exception ex)
            {
                v.VesselDescription = ex.Message; //TODO
            }
            return v;
        }
    }

    public class UnknownVessel : Vessel
    {
        public UnknownVessel()
        {
            this.VesselName = "Unknown Vessel";

        }
    }
}
