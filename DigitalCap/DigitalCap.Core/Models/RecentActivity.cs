using DapperExtensions.Mapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class RecentActivity
    {
        public int ID { get; set; }
        public string Text { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDttm { get; set; }

        public int PIDNumber { get; set; }
        public int ProjectId { get; set; }

        public string VesselName { get; set; }

        public string Description
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CreateBy)
                 || CreateBy.Equals("System", StringComparison.InvariantCultureIgnoreCase))
                    return Text;

                var result = $"{Text} - {CreateBy}";

                return result;
            }
        }
    }

    public class RecentActivityMapper : ClassMapper<RecentActivity>
    {
        public RecentActivityMapper()
        {
            Table("[CAP].[FullActivities]");

            Map(x => x.Description).Ignore();

            AutoMap();
        }
    }
}
