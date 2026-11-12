using DapperExtensions;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class ClientRepository :  IClientRepository
    {
        private readonly ILogger<ProjectRepository> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public ClientRepository(IUnitOfWork unitOfWork, ILogger<ProjectRepository> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<Client>> GetAllClients()
        {
            var connection = _unitOfWork.Connection;
            var clients = await connection.GetListAsync<Client>(
                transaction: _unitOfWork.Transaction);

            return clients.ToList();
        }
    }
}
