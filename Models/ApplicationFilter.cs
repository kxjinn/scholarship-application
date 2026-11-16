using System;

namespace scholarship_Application.Models
{
    // class 'to para sa search options, para hindi masyadong madaming parameters sa Search() method ng ApplicationRepository
    public class ApplicationFilter
    {
        public int? ScholarshipId { get; set; }
        public ApplicationStatus? Status { get; set; }
        public int? CourseId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string Keyword { get; set; }      // student no. or part of a name
    }
}
