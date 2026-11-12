using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Survey
{
    public class Vessel
    {
        [BindProperty]
        public int ID { get; set; }
        [BindProperty]
        public string ReportNo { get; set; }
        [BindProperty]
        public DateTime PortDate { get; set; }
        [BindProperty]
        public DateTime ActualReportStartDate { get; set; }
        [BindProperty]
        public string VesselName { get; set; }
        [BindProperty]
        public int VesselID { get; set; }
        [BindProperty]
        public int ImoNumber { get; set; }
        [BindProperty]
        public string VesselType { get; set; }
        [BindProperty]
        public int YearBuilt { get; set; }
        [BindProperty]
        public string BuiltBy { get; set; }
        [BindProperty]
        public string HullNo { get; set; }
        [BindProperty]
        public string FlagName { get; set; }
        [BindProperty]
        public string Homeport { get; set; }
        [BindProperty]
        public string OfficalNumber { get; set; }
        [BindProperty]
        public string CallSign { get; set; }
        [BindProperty]
        public string PreviousName { get; set; }
        [BindProperty]
        public string OwnerName { get; set; }
        [BindProperty]
        public string Manager { get; set; }
        [BindProperty]
        public float Length { get; set; }
        [BindProperty]
        public float Breadth { get; set; }
        [BindProperty]
        public float DEPTH { get; set; }
        [BindProperty]
        public float Draft { get; set; }
        [BindProperty]
        public string DEADWEIGHT { get; set; }
        [BindProperty]
        public float GrossTons { get; set; }
        [BindProperty]
        public float NetTons { get; set; }
        [BindProperty]
        public string PropulsionType { get; set; }
        [BindProperty]
        public string KW { get; set; }
        [BindProperty]
        public float ShaftRPM { get; set; }
        [BindProperty]
        public string PropulsionManufacturer { get; set; }
        [BindProperty]
        public string GearManufacturer { get; set; }
        [BindProperty]
        public string Classed { get; set; }

        [BindProperty]
        public string ClassNo { get; set; }
        [BindProperty]
        public string ClassNotation { get; set; }
        [BindProperty]
        public string Machinery { get; set; }
        [BindProperty]
        public string Other { get; set; }
        [BindProperty]
        public string Environment { get; set; }
        [BindProperty]
        public string SelectedPrefix { get; set; }
        public string FirstVisitDate { get; set; }
        public string LastVisitDate { get; set; }
    }
}
