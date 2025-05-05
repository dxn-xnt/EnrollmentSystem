using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Student_Enrollment_System.Models
{
    public class Student
    {
        private int Id { get; set; }
        private string FirstName { get; set; }
        private string LastName { get; set; }
        private string MiddleName { get; set; }
        private string HomeAddress { get; set; }
        private string CityAddress { get; set; }
        private string District { get; set; }
        private string Contact { get; set; }
        private string Email { get; set; }
        private int YearLevel { get; set; }
        private string Program { get; set; }
    }
}