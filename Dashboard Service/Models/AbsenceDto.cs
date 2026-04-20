using System;

namespace Dashboard_Service.Models
{
    public class AbsenceDto
    {
        public long Id { get; set; }
        public string EmployeeName { get; set; }
        public string Type { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string Status { get; set; }
    }
}
