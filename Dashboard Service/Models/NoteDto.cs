using System;

namespace Dashboard_Service.Models
{
    public class NoteDto
    {
        public long Id { get; set; }

        public string Title { get; set; }
        public string Text { get; set; }
        public bool Editable { get; set; }
        public long UserId { get; set; }
        public long CreatorId { get; set; }
        public DateTime LastModified { get; set; }
        public bool Disabled { get; set; }
    }
}
