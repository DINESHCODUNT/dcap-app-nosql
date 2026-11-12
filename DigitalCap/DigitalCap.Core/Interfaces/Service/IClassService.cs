using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IClassService
    {
        Task<IEnumerable<ClassModel>> Search();

        Task<string[]> MapFromName(string className1, string className2);
    }
}
