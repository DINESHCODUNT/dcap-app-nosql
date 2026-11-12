using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Tank;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IClientRepository
    {
        Task<List<Client>> GetAllClients();
    }
}
