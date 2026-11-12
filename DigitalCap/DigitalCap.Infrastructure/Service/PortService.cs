using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Service
{
    public class PortService : IPortService
    {
        private readonly IPortRepository _portRepository;

        public PortService(IPortRepository portRepository)
        {
            _portRepository = portRepository;
        }

        public async Task<ServiceResult<IEnumerable<Port>>> Search(string text)
        {
            var result= await _portRepository.Search(text);

            return ServiceResult<IEnumerable<Port>>.Success(result);
        }
    }
}
