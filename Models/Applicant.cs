using System;
using System.ComponentModel;

namespace scholarship_Application.Models
{
    public class Applicant
    {
        public int ApplicantId { get; set; }
        public string StudentNo { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; }        // from the Courses join
        public string CourseName { get; set; }        // navigation properties tawag sa mga ganito
        public int YearLevel { get; set; }
        public string ContactNo { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        [Browsable(false)] // tinry ko lang browsable na ginamit ni febe
        public string FullName
        {
            get
            {
                string middle = string.IsNullOrWhiteSpace(MiddleName) ? "" : " " + MiddleName;
                return LastName + ", " + FirstName + middle;
            }
        }
    }
}
