using System.Web.Mvc;

namespace Student_Enrollment_System.Controllers
{
    public class ProfessorController : Controller
    {
        [HttpGet]
        public ActionResult Dashboard()
        {
            return View("~/Views/Professor/Dashboard.cshtml");
        }
        [HttpGet]
        public ActionResult Classes()
        {
            return View("~/Views/Professor/Classes.cshtml");
        }
    }
}