using Microsoft.Azure.Cosmos;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public interface ICosmosContainerFactory
    {
        Container GetContainer(string containerName);
    }
}
