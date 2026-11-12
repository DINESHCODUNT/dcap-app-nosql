using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.Survey;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class SurveyPhotoPlacementViewModel
    {
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public List<GradingSection> reportSections { get; set; }
        public List<UpskillImageData> ImageData { get; set; }
        public int imo { get; set; }
        public string AssignmentId { get; set; }
        public int ReportTemplateSectionId { get; set; }
        public int CurrentSurveyImage { get; set; } = 0;
        public List<ReportSection> ReportSections { get; set; }
        public List<UpskillImageData> UnplacedImages { get; set; }
    }
}
