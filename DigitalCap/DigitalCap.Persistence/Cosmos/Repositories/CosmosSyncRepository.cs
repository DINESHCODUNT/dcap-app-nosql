using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models.Survey;
using System;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    /// <summary>
    /// Replaces SyncRepository. GetUserInfo is only reached from the desktop (isElectron) branch of ProjectService.GetList.
    /// The SQL version reads table LogInData, which does not exist in the web database, so it always failed:
    /// keep the same failure (logic parity).
    /// </summary>
    public class CosmosSyncRepository : ISyncRepository
    {
        public Task<LoginData> GetUserInfo()
        {
            throw new InvalidOperationException("Invalid object name 'LogInData'.");
        }
    }
}
