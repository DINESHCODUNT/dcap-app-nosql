using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IClassRepository
    {
        Task<IEnumerable<ClassModel>> Search();
    }
}
