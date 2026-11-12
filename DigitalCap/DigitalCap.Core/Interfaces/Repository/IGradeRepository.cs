using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IGradeRepository
    {
        Task<ProjectGrades> GetProjectGrades(int projectId);
        Task UpdateProjectGrades(ProjectGrades projectGrades);
        Task<int> CreateProjectGrades(int projectId);

    }
}
