using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Student_Enrollment_System.Models
{
    public class ScheduleViewModel
    {
        public Schedule Schedule { get; set; }
        public List<TimeSlot> TimeSlot { get; set; }
        
    }
}