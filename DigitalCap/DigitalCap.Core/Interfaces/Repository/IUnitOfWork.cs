using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IUnitOfWork
    {
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }
        string ConnectionString { get; }
        void Begin();
        void Commit();
        void Rollback();
    }
}
