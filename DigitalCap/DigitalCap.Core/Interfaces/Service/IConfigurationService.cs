using DigitalCap.Core.Models;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IConfigurationService
    {
        AppConfiguration GetConfiguration();
    }
}
