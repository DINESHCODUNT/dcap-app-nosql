using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IJsonContainer<T>
    {
        IEnumerable<T> Data { get; }
    }
}
