using DapperExtensions.Mapper;
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

    public class ClientMapper : ClassMapper<Client>
    {
        public ClientMapper()
        {
            Table("[CAP].[Clients]");

            Map(x => x.BillSameAsCapClient).Ignore();
            Map(x => x.BillToCompanyName).Ignore();
            Map(x => x.BillToCompanyAddress).Ignore();
            Map(x => x.BillToCompanyEmail).Ignore();
            Map(x => x.BillToCompanyBillingUrl).Ignore();
            Map(x => x.PocName).Ignore();
            Map(x => x.PocEmail).Ignore();
            Map(x => x.PocPhone).Ignore();
            Map(x => x.Deleted).Ignore();

            AutoMap();
        }
    }
}
