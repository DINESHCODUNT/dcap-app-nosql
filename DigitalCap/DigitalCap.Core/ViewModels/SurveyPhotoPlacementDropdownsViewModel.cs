using DigitalCap.Core.Models.Survey;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class SurveyPhotoPlacementDropdownsViewModel
    {
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }

        [Display(Name = "Image Description")]
        public List<ImageDescriptionUI> ImageDescription { get; set; }

        [Display(Name = "Image Card")]
        public List<ImageCards> ImageCard { get; set; }

        [Display(Name = "Condition")]
        public List<CurrentCondition> CurrentCondition { get; set; }

    }

    public class PhotoPlacementSequence
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public PhotoPlacementCard CaptureCard { get; set; } = new PhotoPlacementCard();
        public PhotoPlacementCard ImageDescriptionCard { get; set; } = new PhotoPlacementCard();
        public PhotoPlacementCard AdditionalDescriptionCard { get; set; } = new PhotoPlacementCard();
        public PhotoPlacementCard ConditionCard { get; set; } = new PhotoPlacementCard();
    }

    public class PhotoPlacementCard
    {
        public string Id { get; set; }
        public string SequenceId { get; set; }
        public string Label { get; set; }
    }

}
