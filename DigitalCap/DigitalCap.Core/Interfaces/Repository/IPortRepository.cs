using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IPortRepository
    {
        Task<IEnumerable<Port>> Search(string searchText);
        Task<Port?> GetById(int id);
    }
}
