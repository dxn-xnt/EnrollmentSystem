namespace Student_Enrollment_System.Models
{
    public class Enrollment
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Date { get; set; }
        public int YearLevel { get; set; }
        public int Semester { get; set; }
        public int StudentId { get; set; }
        public string CourseCode { get; set; }
        public string AcademicYear { get; set; }
    }
}