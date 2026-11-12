using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class PortRepository : IPortRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;

        public PortRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public void Commit()
        {
            _unitOfWork?.Commit();
        }

        public async Task<IEnumerable<Port>> Search(string searchText)
        {
            return await Connection.QueryAsync<Port>(
                sql: "CAP.Port_Search",
                new { SearchText = searchText },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );
        }

        public async Task<Port?> GetById(int id)
        {
            return await Connection.QueryFirstOrDefaultAsync<Port>(
                @"SELECT  PortId AS Id,PortCode AS Code,PortName AS Name,CountryCode FROM Core.PortOfCall WHERE PortId = @Id",
                new { Id = id }
            );
        }
    }
}
