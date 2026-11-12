using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class TemplateRepository : ITemplateRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;
        public TemplateRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        public void Commit()
        {
            _unitOfWork?.Commit();
        }

        public async Task<byte[]> PopulateTemplate(int projectId)
        {
            try
            {
                var result = await Connection.QueryFirstOrDefaultAsync<byte[]>(
                    sql: "[upskill].[sp_PopulateTemplate]",
                    param: new { ProjectId = projectId },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                return result ?? new byte[0];
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }
    }
}
