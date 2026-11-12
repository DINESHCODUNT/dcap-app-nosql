using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Tank
{
    public class TankTypes
    {
        public int Id { get; set; }
        public string TankType { get; set; }

        public string TankName { get; set; }
        public int TemplateId { get; set; }
    }
}
