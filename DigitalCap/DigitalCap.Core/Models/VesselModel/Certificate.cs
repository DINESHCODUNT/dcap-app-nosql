using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class Link
    {
        public string Rel { get; set; }
        public string Href { get; set; }
        public string Action { get; set; }
        public List<string> Types { get; set; }
    }

    public class Certificate : CachedData
    {
        public string ServiceType { get; set; }
        public string Id { get; set; }
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Abbr { get; set; }
        public string Term { get; set; }
        public string FlagName { get; set; }
        public string Deadweight { get; set; }
        public string IssueDate { get; set; }
        public string ExpiryDate { get; set; }
        public string ExtensionDate { get; set; }
        public string PlaceOfIssue { get; set; }
        public string IssuedBy { get; set; }
        public string PersonName { get; set; }
        public string Status { get; set; }
        public string StatusDate { get; set; }
        public Link Link { get; set; }
    }
}
