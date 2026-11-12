using System;
using System.Collections.Generic;

namespace DigitalCap.Core.DTO
{
    public class ActivityLogDto
    {
        public List<ActivityGroupDto> Groups { get; set; } = new List<ActivityGroupDto>();
    }

    public class ActivityGroupDto
    {
        public string DateLabel { get; set; }
        public string DateText { get; set; }
        public List<ActivityItemDto> Activities { get; set; } = new List<ActivityItemDto>();
    }

    public class ActivityItemDto
    {
        public int Id { get; set; }
        public string Type { get; set; } // create, modify, assigned, status, complete
        public string Text { get; set; }
        public string Time { get; set; }
        public string Tag { get; set; }
        public ActivityUserDto User { get; set; }
        public string Entity { get; set; } // Project ID or reference
        public List<ActivityMetaDto> Metadata { get; set; } = new List<ActivityMetaDto>();
    }

    public class ActivityUserDto
    {
        public string Name { get; set; }
        public string Initials { get; set; }
        public string AvatarColor { get; set; }
    }

    public class ActivityMetaDto
    {
        public string Type { get; set; } // location, file, progress, etc.
        public string Value { get; set; }
        public string Icon { get; set; }
    }
}
