using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class BreadCrumbViewModel
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public bool ClientPage { get; set; }
        public List<BreadCrumb> BreadCrumbs { get; set; } = new List<BreadCrumb>();

        public BreadCrumbViewModel(string projectId, string projectName, string displayName = "", bool clientPage = false)
        {
            ProjectId = projectId;
            ProjectName = projectName;
            ClientPage = clientPage;
            var breadCrumb = new BreadCrumb(displayName);
            BreadCrumbs.Add(breadCrumb);
        }

        public BreadCrumbViewModel(string projectId, string projectName, BreadCrumb breadCrumb, BreadCrumb breadCrumb2 = null)
        {
            ProjectId = projectId;
            ProjectName = projectName;

            BreadCrumbs.Add(breadCrumb);
            BreadCrumbs.Add(breadCrumb2);
        }

        public BreadCrumbViewModel(string projectName)
        {
            ProjectName = projectName;
        }

        public BreadCrumbViewModel(string projectId, string projectName)
        {
            ProjectId = projectId;
            ProjectName = projectName;
        }
    }

    public class BreadCrumb
    {
        public string Url { get; set; }
        public string DisplayName { get; set; }

        public BreadCrumb(string displayName, string url = "")
        {
            DisplayName = displayName;
            Url = url;
        }
    }
}


    

