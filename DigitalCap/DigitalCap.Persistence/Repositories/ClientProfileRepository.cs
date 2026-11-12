using Dapper;
using DapperExtensions;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.DTO;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class ClientProfileRepository :  IClientProfileRepository
    {
        private readonly ILogger<ClientProfileRepository> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public ClientProfileRepository(IUnitOfWork unitOfWork, ILogger<ClientProfileRepository> logger) 
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Client>> GetAllClientsAsync(CancellationToken cancellationToken = default)
        {
            var connection = _unitOfWork.Connection;

            var client = await connection.GetListAsync<Client>(
                transaction: _unitOfWork.Transaction);
            
            return client.ToList();
        }

        public async Task AddAsync(Client client)
        {
            const string sql = """
            INSERT INTO [CAP].[Clients]
            (Id, CompanyName, CompanyAddress, CompanyBillingEmail, CompanyBillingSystemUrl, Wcn)
            VALUES
            (@Id, @CompanyName, @CompanyAddress, @CompanyBillingEmail, @CompanyBillingSystemUrl, @Wcn)
            """;

            await _unitOfWork.Connection.ExecuteAsync(
                sql,
                new
                {
                    Id = client.Id,
                    CompanyName = client.CompanyName,
                    CompanyAddress = client.CompanyAddress,
                    CompanyBillingEmail = client.CompanyBillingEmail,
                    CompanyBillingSystemUrl = client.CompanyBillingSystemUrl,
                    Wcn = client.Wcn
                },
                transaction: _unitOfWork.Transaction
            );

            _unitOfWork.Commit();
        }

        //Get Client Admin User Details
        public async Task<IEnumerable<ClientAdminDto>> GetClientAdminsAsync(Guid clientId)
        {
            const string sql = @"
            SELECT
            ua.FirstName,
            ua.LastName,
            au.Email,
            r.Name AS UserRole
            FROM   UserAccounts         ua
            INNER  JOIN AspNetUsers     au ON ua.AspNetUserId = au.Id
            LEFT   JOIN AspNetUserRoles ur ON au.Id           = ur.UserId
            LEFT   JOIN AspNetRoles     r  ON ur.RoleId       = r.Id
            WHERE  ua.ClientId = @ClientId
            ORDER  BY ua.FirstName, ua.LastName";

            return await _unitOfWork.Connection.QueryAsync<ClientAdminDto>(
                sql,
                new { ClientId = clientId },
                transaction: _unitOfWork.Transaction
            );
        }


        public async Task<Client?> GetClientAsync(Guid Id)
        {
            var connection = _unitOfWork.Connection;
            var clients = await connection.GetAsync<Client>(Id,transaction: _unitOfWork.Transaction);
            return clients;
        }

        public async Task<List<Client>> GetAllClients()
        {
            var connection = _unitOfWork.Connection;
            var clients = await connection.GetListAsync<Client>(
                transaction: _unitOfWork.Transaction);
           
            return clients.ToList();

        }

        public async Task<Guid> SaveClientProfileAsync(Client client)
        {
            var connection = _unitOfWork.Connection;
            var transaction = _unitOfWork.Transaction;


            if (client.Id == Guid.Empty)
            {
                
                // Insert
                var newId = await connection.InsertAsync(
            client,
            transaction: transaction);
                _unitOfWork.Commit();
                return newId;
            }
            else
            {
                
                var sql = @"
                UPDATE CAP.Clients
                SET 
                CompanyName = @CompanyName,
                CompanyAddress = @CompanyAddress,
                CompanyBillingEmail = @CompanyBillingEmail,
                CompanyBillingSystemUrl = @CompanyBillingSystemUrl,
                Wcn = @Wcn
                WHERE Id = @Id";

                var affectedRows = await connection.ExecuteAsync(sql, new
                {
                    client.CompanyName,
                    client.CompanyAddress,
                    client.CompanyBillingEmail,
                    client.CompanyBillingSystemUrl,
                    client.Wcn,
                    client.Id
                }, transaction);

                if (affectedRows == 0)
                    throw new Exception("Update failed. No rows affected.");

                _unitOfWork.Commit();
                return client.Id;
            }

            
        }

        public async Task DeleteClientAsync(Guid Id)
        {
            var connection = _unitOfWork.Connection;
            var sql = "DELETE FROM [CAP].[Clients] WHERE Id = @Id";
            var rowsAffected = await connection.ExecuteAsync(
        sql,new { Id = Id },
        transaction: _unitOfWork.Transaction);
            
            _unitOfWork.Commit();
        }

    }
}
