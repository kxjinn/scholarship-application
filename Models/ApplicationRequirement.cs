using System;

namespace scholarship_Application.Models
{
    public class ApplicationRequirement
    {
        public int ApplicationId { get; set; }
        public int RequirementId { get; set; }
        public string RequirementName { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTime? DateSubmitted { get; set; }
        public int? VerifiedBy { get; set; }
        public string VerifiedByName { get; set; }
    }
}
