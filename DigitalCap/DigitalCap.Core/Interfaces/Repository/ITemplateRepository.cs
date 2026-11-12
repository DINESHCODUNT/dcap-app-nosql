using DigitalCap.Core.Models.Reporting;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface ITemplateRepository
    {
        Task<byte[]> PopulateTemplate(int projectId);
    }
}
