using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class Client 
    {
        public Guid Id { get; set; }
        public string? Wcn { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyBillingEmail { get; set; }
        public string CompanyBillingSystemUrl { get; set; }
        public Boolean BillSameAsCapClient { get; set; }// = true;
        public string BillToCompanyName { get; set; }
        public string BillToCompanyAddress { get; set; }
        public string BillToCompanyEmail { get; set; }
        public string BillToCompanyBillingUrl { get; set; }
        public string PocName { get; set; }
        public string PocEmail { get; set; }
        public string PocPhone { get; set; }
        public bool Deleted { get; set; }
    }
}
