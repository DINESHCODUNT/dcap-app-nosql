using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Configuration
{
    public static class ConfigurationExtensions
    {
        public static string GetABSConnectionString(this IConfiguration source)
        {
            var result = source[ConfigurationConstants.ConfigurationKeys.Database.ABSConnectionString];

            return result;
        }

        public static string GetBlobStorageConnectionString(this IConfiguration source)
        {
            var result = source[ConfigurationConstants.ConfigurationKeys.BlobStorage.ConnectionString];

            return result;
        }

        public static string GetBlobStorageContainerName(this IConfiguration source)
        {
            var result = source[ConfigurationConstants.ConfigurationKeys.BlobStorage.ContainerName];

            return result;
        }

        public static string GetRequestAccessEmail(this IConfiguration source)
        {
            return source[ConfigurationConstants.EmailAddresses.RequestAccessEmail];
        }
    }
}
