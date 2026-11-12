using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class ClassRepository : IClassRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;

        public ClassRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public void Commit()
        {
            _unitOfWork?.Commit();
        }

        public async Task<IEnumerable<ClassModel>> Search()
        {
            return await Connection.QueryAsync<ClassModel>(
                sql: "CAP.Class_Search",
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );
        }
    }
}
