using DapperExtensions.Mapper;
using DigitalCap.Core.Enumerations;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class ProjectFile
    {
        public int Id { get; set; }

        [UIHint("ProjectDataTypeDropdown")]
        [Display(Name = "Data Type")]
        public FileDataType DataType { get; set; }
        public int ProjectId { get; set; }
        /// <summary>Optional; maps to CAP.Create_File @TaskId when set (otherwise null).</summary>
        public int? TaskId { get; set; }
        public string FullName => Name == null && Extension == null
                                    ? null
                                    : string.Join("", Name, Extension);

        public string Name { get; set; }
        public string Extension { get; set; }
        public string MimeType { get; set; }
        [Display(Name = "Upload User")]
        public string UploadedBy { get; set; }
        public string StorageKey { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
        [UIHint("ProjectAvailableToClientDropdown")]
        public bool AvailableToClient { get; set; }
        public bool SurveyImage { get; set; }

        public ProjectFile() { }

        public ProjectFile(IFormFile source)
        {
            if (source == null)
                return;

            Name = Path.GetFileNameWithoutExtension(source.FileName);
            Extension = Path.GetExtension(source.FileName);
            MimeType = source.ContentType;
        }
    }

    public class ProjectFileMapper : ClassMapper<ProjectFile>
    {
        public ProjectFileMapper()
        {
            Table("[CAP].[Files]");
            Map(x => x.FullName).Ignore();
            AutoMap();
        }
    }
}
