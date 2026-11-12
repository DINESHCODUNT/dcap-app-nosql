using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Tank
{
    public class TankImageCard
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
        public int CardNumber { get; set; }
        public string CardName { get; set; }
        public int DescriptionId { get; set; }
        public string AdditionalDescription { get; set; }
        public int? CurrentCondition { get; set; }
        public int ImageId { get; set; }
        public string src { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdatedDttm { get; set; }
        public bool IsActive { get; set; }
        public bool IsSync { get; set; }
    }

    public class TankCardComparer : IEqualityComparer<TankImageCard>
    {
        public bool Equals(TankImageCard x, TankImageCard y)
        {
            if (x == null || y == null) return false;
            return x.CardNumber == y.CardNumber;
        }

        public int GetHashCode(TankImageCard obj)
        {
            return obj.CardNumber.GetHashCode();
        }
    }

}
