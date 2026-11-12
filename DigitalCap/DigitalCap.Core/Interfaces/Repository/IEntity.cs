using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IEntity<TKey>
    {
        TKey Id { get; set; }
        bool Deleted { get; set; }
    }

}
