using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IAuditableEntity<TKey> : IEntity<TKey>
    {
        string? CreatedBy { get; set; }
        DateTime? CreatedAt { get; set; }
        string? UpdatedBy { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}
