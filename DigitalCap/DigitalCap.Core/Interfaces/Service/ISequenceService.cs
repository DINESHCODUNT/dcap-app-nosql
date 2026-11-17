using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface ISequenceService
    {
        Task<int> GetNextAsync(string containerName, CancellationToken ct = default);
    }
}
