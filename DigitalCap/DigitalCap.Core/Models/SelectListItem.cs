using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class SelectListItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }

    // Collection wrapper (MVC-free)
    public class SelectList : List<SelectListItem>
    {
        public SelectList() { }
        public SelectList(IEnumerable<SelectListItem> items) : base(items) { }
    }
}
