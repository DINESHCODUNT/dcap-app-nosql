using DigitalCap.Core.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public abstract class EntityBase<TKey> : IEntity<TKey>
    {
        public TKey Id { get; set; }
        public bool Deleted { get; set; }
    }

}
