using DigitalCap.Core.Models.Survey;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface ISyncRepository
    {
        Task<LoginData> GetUserInfo();
    }
}
