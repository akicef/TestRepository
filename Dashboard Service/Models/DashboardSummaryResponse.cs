using System;
using System.Collections.Generic;

namespace Dashboard_Service.Models
{
    public class DashboardSummaryResponse
    {
        public string Role { get; set; }
        public DateTimeOffset GeneratedAt { get; set; }
        public List<KpiCardDto> Kpis { get; set; }
        public List<TableSectionDto> Tables { get; set; }
    }
}
