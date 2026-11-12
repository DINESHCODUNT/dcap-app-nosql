using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class UpskillImageData
    {
        public int Id { get; set; }
        public int ImageId { get; set; }
        public string Base64String { get; set; }
        public DateTime? CreateDttm { get; set; }
        public DateTime? UpdateDttm { get; set; }
        public int ProjectId { get; set; }
    }
    public class DownloadImageResultDto
    {
        public string Base64 { get; set; }
        public string ContentType { get; set; }
    }
}
