using System.Web.Mvc;

namespace Student_Enrollment_System.Controllers
{
    public class ProgramHeadController : Controller
    {
        // GET
        public ActionResult Dashboard()
        {
            return View("~/Views/Program-Head/Dashboard.cshtml");
        }
        public ActionResult Students()
        {
            return View("~/Views/Program-Head/ViewStudentList.cshtml");
        }
        public ActionResult Enrollments()
        {
            return View("~/Views/Program-Head/EnrollmentApproval.cshtml");
        }
        public ActionResult Schedules()
        {
            return View("~/Views/Program-Head/SetSchedules.cshtml");
        }
        public ActionResult StudentManagement()
        {
            return View("~/Views/Program-Head/StudentManagement.cshtml");
        }
        public ActionResult ClassManagement()
        {
            return View("~/Views/Program-Head/ClassManagement.cshtml");
        }
    }
}