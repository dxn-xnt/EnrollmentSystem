using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Student_Enrollment_System.Models
{
    public class Schedule
    {
        public int Id { get; set; }
        public string CourseCode { get; set; }
        public string RoomId { get; set; }
        public string BlockSectionCode { get; set; }
        public string ProfessorId { get; set; }
    }
}