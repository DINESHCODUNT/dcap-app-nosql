using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IUserRepository
    {
        Task<List<ApplicationDto>> GetAllUsers();
        Task<string> GetLoggedInUserName();


    }
}
