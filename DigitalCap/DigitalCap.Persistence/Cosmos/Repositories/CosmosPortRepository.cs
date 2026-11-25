using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosPortRepository : CosmosRepositoryBase<PortOfCallDocument>, IPortRepository
    {
        public CosmosPortRepository(ICosmosContainerFactory containerFactory, ILogger<CosmosPortRepository> logger)
            : base(containerFactory, CosmosContainers.core_PortOfCall, logger)
        {
        }

        /// <summary>
        /// Replaces stored procedure [CAP].[Port_Search].
        /// </summary>
        public async Task<IEnumerable<Port>> Search(string searchText)
        {
            QueryDefinition query;
            if (searchText == null)
            {
                query = new QueryDefinition("SELECT * FROM c");
            }
            else
            {
                if (searchText.Length > 500)
                {
                    searchText = searchText.Substring(0, 500);
                }

                var pattern = LikeToRegex(searchText);
                query = new QueryDefinition("SELECT * FROM c WHERE RegexMatch(c.PortName, @pattern, 'i')")
                    .WithParameter("@pattern", pattern);
            }

            var documents = await QueryAsync(query);
            return documents.Select(d => new Port
            {
                Id = d.PortId,
                Code = d.PortCode!,
                Name = d.PortName!
                // CountryCode is not returned by CAP.Port_Search: leave null
            }).ToList();
        }

        private static string LikeToRegex(string input)
        {
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < input.Length)
            {
                char ch = input[i];
                if (ch == '%')
                {
                    sb.Append(".*");
                    i++;
                }
                else if (ch == '_')
                {
                    sb.Append(".");
                    i++;
                }
                else if (ch == '[')
                {
                    int closeIdx = input.IndexOf(']', i + 1);
                    if (closeIdx > i + 1)
                    {
                        var bracketContent = input.Substring(i, closeIdx - i + 1);
                        sb.Append(bracketContent);
                        i = closeIdx + 1;
                    }
                    else
                    {
                        sb.Append(@"\[");
                        i++;
                    }
                }
                else if (@"\^$.|?*+(){}".Contains(ch))
                {
                    sb.Append('\\').Append(ch);
                    i++;
                }
                else
                {
                    sb.Append(ch);
                    i++;
                }
            }
            return ".*" + sb.ToString() + ".*";
        }

        /// <summary>
        /// Replaces inline query: SELECT PortId AS Id, PortCode AS Code, PortName AS Name, CountryCode FROM Core.PortOfCall WHERE PortId = @Id.
        /// </summary>
        public async Task<Port?> GetById(int id)
        {
            var idString = id.ToString();
            var doc = await ReadAsync(idString, new PartitionKey(idString));
            if (doc == null)
            {
                return null;
            }

            return new Port
            {
                Id = doc.PortId,
                Code = doc.PortCode!,
                Name = doc.PortName!,
                CountryCode = doc.CountryCode!
            };
        }
    }
}
