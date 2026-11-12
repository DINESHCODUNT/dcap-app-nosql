using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Helpers.Extensions;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using AgreementOwnerEnum = DigitalCap.Core.Enumerations.AgreementOwner;
using CAPRegionEnum = DigitalCap.Core.Enumerations.CapRegion;


namespace DigitalCap.Core.Helpers.Constants
{
    public static class FormValues
    {
        public static IEnumerable<string> CAPScope => new[]
        {
            "Hull & Machinery",
            "Hull Only",
            "Machinery Only",
            "Pre-CAP",
            "Other"
        };

        public static IEnumerable<string> ABSRoles => new[]
        {
            "CAP User",
            "Contributor (CAP Coordinator)",
            "Admin (CAP HQ)"
        };

        public static IEnumerable<string> ClientRoles => new[]
        {
            "Client Admin",
            "Client User"
        };
        public static SelectList RepairCondition => new SelectList
{
    new SelectListItem { Text = "Before Repair", Value = "id1" },
    new SelectListItem { Text = "After Repair", Value = "id2" }
};


        public static SelectList ModificationCondition => new SelectList
        {
            new SelectListItem { Text = "Before Modification", Value = "id1" },
            new SelectListItem { Text = "After Modification", Value = "id2" }
        };
        public static IEnumerable<string> CAPRegion => new[]
        {
            CAPRegionEnum.AmericasEurope.GetDescription(),
            CAPRegionEnum.Greece.ToString(),
            CAPRegionEnum.Singapore.ToString(),
            CAPRegionEnum.MiddleEast.GetDescription()
        };

        public static IEnumerable<string> AgreementOwner => new[]
        {
            AgreementOwnerEnum.ABSGroup.GetDescription(),
            AgreementOwnerEnum.ABSAdvancedSolutions.GetDescription()
        };

        public static IEnumerable<SelectListItem> MonthBuilt =>
            Enumerable.Range(1, 12)
                .ToSelectListItems(
                    getText: x => new DateTime(1900, x, 1).ToString("MMM"));

        public static IEnumerable<SelectListItem> AdditionalFilesDataTypes => new[]
        {
            FileDataType.ClientDeliverables,
            FileDataType.InternalDocuments,
            FileDataType.InternalInvoicing,
        }.ToSelectListItems(
            getValue: x => ((int)x).ToString(),
            getText: x => x.ToString()
        );

        public static IEnumerable<AvailableToClient> availableToClients => new[]
        {
            new AvailableToClient() { Text = "No", Value = false },
            new AvailableToClient() { Text = "Yes", Value = true }
        };

        public class AvailableToClient
        {
            public string Text { get; set; }
            public bool Value { get; set; }
        }
    }
}
