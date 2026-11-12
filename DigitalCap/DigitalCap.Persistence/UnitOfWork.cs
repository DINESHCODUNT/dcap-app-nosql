using DigitalCap.Core.Interfaces.Repository;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DigitalCap.Persistence
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly string _connectionString;
        private bool _disposed;

        public IDbConnection Connection { get; private set; }
        public IDbTransaction Transaction { get; private set; }
        public string ConnectionString => _connectionString;
        public UnitOfWork(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");

            Connection = new SqlConnection(_connectionString);
        }

        public void Begin()
        {
            if (Connection.State != ConnectionState.Open)
                Connection.Open();

            // Dispose existing transaction if it exists before creating a new one
            // This prevents "pending local transaction" errors
            if (Transaction != null)
            {
                try
                {
                    Transaction.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
                Transaction = null;
            }

            Transaction = Connection.BeginTransaction();
        }

        public void Commit()
        {
            try
            {
                Transaction?.Commit();
            }
            finally
            {
                Transaction?.Dispose();
                Transaction = null;
            }
        }

        public void Rollback()
        {
            try
            {
                Transaction?.Rollback();
            }
            finally
            {
                Transaction?.Dispose();
                Transaction = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            Transaction?.Dispose();
            Connection?.Close();
            Connection?.Dispose();

            _disposed = true;
        }
    }
}