using System.ComponentModel;

namespace scholarship_Application.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }

        [Browsable(false)]
        public string Display
        {
            get { return CourseCode + " - " + CourseName; }
        }
    }
}
