using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Reporting
{
    public class ReportFile : ProjectFile
    {
        public ReportFile()
            : base()
        {
            DataType = DigitalCap.Core.Enumerations.FileDataType.Reports;
        }
    }
}
