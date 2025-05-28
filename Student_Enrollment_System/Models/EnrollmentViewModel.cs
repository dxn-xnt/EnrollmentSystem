using System.Collections.Generic;
using Student_Enrollment_System.Models;

namespace Student_Enrollment_System.Models
{
    public class EnrollmentViewModel
    {
        public Student Student { get; set; }
        public Enrollment Enrollment { get; set; }
        public List<EnrollingCourse> EnrollingCourses{ get; set; }
    }
}

