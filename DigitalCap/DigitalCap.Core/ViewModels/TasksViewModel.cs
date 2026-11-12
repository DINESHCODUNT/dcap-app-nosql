using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class TasksViewModel
    {
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }

        public List<Section> Sections { get; set; }

        public TasksViewModel(int? projectId, IEnumerable<SingleTask> tasks)
        {
            ProjectId = projectId;
            CreateSections(tasks);
        }

        private void CreateSections(IEnumerable<SingleTask> tasks)
        {
            Sections = new List<Section>();
            List<string> createdSectionNames = new List<string>();
            Section currentSection = null;
            foreach (var task in tasks)
            {
                if (!string.IsNullOrWhiteSpace(task.Type))
                {
                    if (!createdSectionNames.Contains(task.Type))
                    {
                        createdSectionNames.Add(task.Type);
                        currentSection = new Section(task.Type);
                        Sections.Add(currentSection);
                    }
                    else
                    {
                        if (currentSection.SectionName != task.Type)
                        {
                            currentSection = Sections.Find(s => s.SectionName == task.Type);
                        }
                    }
                    currentSection.Tasks.Add(task);
                }
            }
        }
    }

    public class Section
    {
        public string SectionName { get; set; }
        public string GridName { get; set; }

        public List<SingleTask> Tasks { get; set; }

        public Section(string name)
        {
            SectionName = name;
            GridName = System.Text.RegularExpressions.Regex.Replace(SectionName, @"\W", "");
            Tasks = new List<SingleTask>();
        }
    }
}
