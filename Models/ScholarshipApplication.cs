using System;

namespace scholarship_Application.Models
{
    public class ScholarshipApplication
    {
        public int ApplicationId { get; set; }

        public int ApplicantId { get; set; }
        public string ApplicantName { get; set; }
        public string StudentNo { get; set; }
        public string CourseCode { get; set; }

        public int ScholarshipId { get; set; }
        public string ScholarshipName { get; set; }

        public ApplicationStatus Status { get; set; } // enum 'to guys, nasa Enums.cs
        public string StatusName { get; set; }

        public string AcademicYear { get; set; }         // 2026-2027 type shit
        public decimal Gwa { get; set; }                 // value at the time of application
        public decimal FamilyIncome { get; set; }        // hindi nullable 'yung dalawa kasi required dito
        public DateTime DateApplied { get; set; }

        public int EncodedBy { get; set; }
        public string EncodedByName { get; set; }
    }
}
