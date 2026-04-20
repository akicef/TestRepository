using System.Collections.Generic;

namespace Dashboard_Service.Models
{
    public class TableSectionDto
    {
        public string Key { get; set; }
        public string Title { get; set; }
        // Structured columns with a machine-friendly key and human-friendly title
        public List<ColumnDto> Columns { get; set; }
        // Rows are dictionaries keyed by the column key for clarity and stability
        public List<Dictionary<string, object>> Rows { get; set; }
    }
}
