using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Service
{
    public class SecurityClientService : ISecurityClientService
    {
        private readonly ISecurityClientRepository _securityClientRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SecurityClientService(
            ISecurityClientRepository securityClientRepository,
            IUnitOfWork unitOfWork)
        {
            _securityClientRepository = securityClientRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<IReadOnlyList<ClientModel>>> GetAllClientsAsync()
        {
            var result = await _securityClientRepository.GetAllClientsAsync();

            return ServiceResult<IReadOnlyList<ClientModel>>
                .Success(result.ToList());
        }
    }
}
