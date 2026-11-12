using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class ReportRepository : IReportRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;

        public ReportRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public void Commit()
        {
            _unitOfWork?.Commit();
        }

        public async Task<byte[]> ExportAllProjectsAsExcelSpreadsheet()
        {
            // Note: Excel generation is handled in ReportService (business logic layer)
            // Repository layer is for data access only
            // This method is kept to satisfy IReportRepository interface
            // but actual implementation is in ReportService
            return await Task.FromResult(Array.Empty<byte>());
        }

        public async Task<byte[]> ExportAllProjectsGridAsExcelSpreadsheet()
        {
            // Note: Excel generation is handled in ReportService (business logic layer)
            // Repository layer is for data access only
            // This method is kept to satisfy IReportRepository interface
            // but actual implementation is in ReportService
            return await Task.FromResult(Array.Empty<byte>());
        }

        public async Task<List<Dictionary<string, string>>> ExportProjectsPartial(int minRange, int maxRange)
        {
            // Note: Partial export logic is handled in ReportService (business logic layer)
            // Repository layer is for data access only
            // This method is kept to satisfy IReportRepository interface
            // but actual implementation is in ReportService
            return await Task.FromResult(new List<Dictionary<string, string>>());
        }
    }
}
