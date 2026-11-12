using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IProjectAPIService
    {
        Task<ServiceResult<ProjectSummary>> GetSummary(int projectId);
    }
}
