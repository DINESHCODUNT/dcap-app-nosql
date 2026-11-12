using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.Models.ABSPlatformModel
{
    public class UserRequest
    {
        [Key]
        public string Id { get; set; }

        [Display(Name = "Submitted By")]
        public string SubmittedBy { get; set; }
        [Display(Name = "Submitted On")]
        public DateTime SubmittedOn { get; set; }
        [Display(Name = "Request")]
        public string RequestType { get; set; }
        public string RequestTarget { get; set; }
        [Display(Name = "Request Details")]
        public string RequestDetails { get; set; }
        [Display(Name = "Actioned By")]
        public string ActionedBy { get; set; }
        [Display(Name = "Actioned On")]
        public DateTime? ActionedOn { get; set; }
        [Display(Name = "Action Details")]
        public string ActionDetails { get; set; }
        [Display(Name = "Status")]
        public string CurrentStatus { get; set; }

        public UserRequest()
        {
            SubmittedOn = DateTime.Now;
            CurrentStatus = DigitalCap.Core.Constants.AppConstants.OPEN;
        }
    }
}
