using System;

namespace Dashboard_Service.Models
{
    public class InvoiceDto
    {
        public long Id { get; set; }
        public string PartyName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
