using System.Collections.Generic;
using System.Linq;

namespace DigitalCap.WebApi.Models
{
    public class DataSourceRequest
    {
        public bool? RefreshStalePercentComplete { get; set; }

        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public List<SortDescriptor>? Sort { get; set; }
        public FilterDescriptor? Filter { get; set; }
        public List<GroupDescriptor>? Group { get; set; }
    }

    public class SortDescriptor
    {
        public string Field { get; set; }
        public string Dir { get; set; }
    }

    public class FilterDescriptor
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        public string Logic { get; set; }
        public List<FilterDescriptor> Filters { get; set; }
    }

    public class GroupDescriptor
    {
        public string Field { get; set; }
        public string Dir { get; set; }
        public List<object> Aggregates { get; set; }
    }

    public class DataSourceResult<T>
    {
        public List<T> Data { get; set; }
        public int Total { get; set; }
        public object Aggregates { get; set; }
    }
}