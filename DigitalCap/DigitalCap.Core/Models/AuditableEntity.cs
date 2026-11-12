using DigitalCap.Core.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class AuditableEntity<TKey> : EntityBase<TKey>, IAuditableEntity<TKey>
    {
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
