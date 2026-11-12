using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Configuration
{
    public static class ConfigurationConstants
    {
        public const int ApplicationId = 3;

        public static class ConfigurationKeys
        {
            public static class Database
            {
                public const string ABSConnectionString = "AppDatabase";
            }

            public static class BlobStorage
            {
                public const string ConnectionString = "Blob:ConnectionString";
                public const string ContainerName = "Blob:ContainerName";
            }

        }
        public static class StoredProcedures
        {
            public static class UserAccounts
            {
                public static readonly string GetByAspNetId = "sp_UserAccounts_GetByAspNetId";
                public static readonly string GetByAspNetId_ActiveOrDeleted = "sp_UserAccounts_GetByAspNetId_ActiveOrDeleted";
                public static readonly string GetUsersInRole = "sp_UserAccounts_GetUsersInRole";
                public static readonly string GetUsersWithPermission = "sp_UserAccounts_GetUsersWithPermission";
                public static readonly string GetUsersInRoleForClient = "sp_UserAccounts_GetUsersInRoleForClient";
                public static readonly string GetAbsUsers = "sp_UserAccounts_GetAbsUsers";
                public static readonly string GetClientUsers = "sp_UserAccounts_GetClientUsers";
            }
        }
        public static class Errors
        {
            public static class Views
            {
                public static readonly string GeneralErrorFieldName = "General";
            }
        }

        public static class EmailAddresses
        {
            public static readonly string RequestAccessEmail = "RequestAccessEmail";
        }
    }

}
