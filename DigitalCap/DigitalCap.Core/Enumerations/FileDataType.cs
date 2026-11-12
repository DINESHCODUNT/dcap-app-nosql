using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.Enumerations
{
    public enum FileDataType
    {
        [Description("")]
        Empty = 0,

        [Description("Technical Task")]
        TechnicalTask = 1,

        [Description("Invoice Task")]
        InvoiceTask = 2,

        [Description("Correspondence – Internal")]
        InternalCorrespondence = 3,

        [Description("Correspondence – Client")]
        ClientCorrespondence = 4,

        [Description("Additional Vessel or Engineering Documents/Records")]
        AdditionalVesselOrEngineeringDocumentsOrRecords = 5,

        [Description("Photos")]
        Photos = 6,

        [Description("Misc. Documents")]
        MiscDocuments = 7,

        [Description("Reports")]
        Reports = 8,

        [Description("Client Deliverables")]
        ClientDeliverables = 9,

        [Description("Internal Documents")]      
        InternalDocuments = 10,

        [Description("Internal Invoicing")]        
        InternalInvoicing = 11,

        [Description("Vessel Documents")]       
        VesselDocuments = 12,
    }
}
