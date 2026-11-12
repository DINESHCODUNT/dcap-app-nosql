using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int Total { get; set; }
    }
}
