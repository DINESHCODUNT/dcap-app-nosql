using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IPortService
    {
        Task<ServiceResult<IEnumerable<Port>>> Search(string text);
    }
}
