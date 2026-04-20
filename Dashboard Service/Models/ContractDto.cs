using System;

namespace Dashboard_Service.Models
{
    public class ContractDto
    {
        public long Id { get; set; }
        public string PartyName { get; set; }
        public string Status { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
