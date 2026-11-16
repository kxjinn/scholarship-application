using System;

namespace scholarship_Application.Models
{
    public class StatusHistoryEntry
    {
        public int HistoryId { get; set; }
        public int ApplicationId { get; set; }
        public ApplicationStatus Status { get; set; }
        public string StatusName { get; set; }
        public int ChangedBy { get; set; }
        public string ChangedByName { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Remarks { get; set; }
    }
}
