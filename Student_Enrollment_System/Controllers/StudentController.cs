using System.Web.Mvc;

namespace Student_Enrollment_System.Controllers
{
    public class StudentController : Controller
    {
        // GET: /Student/Dashboard
        [HttpGet]
        public ActionResult Dashboard()
        {
            return View("~/Views/Student/Dashboard.cshtml");
        }
        [HttpGet]
        public ActionResult ProfileView()
        {
            // View is located at Views/Account/LogIn.cshtml
            return View("~/Views/Student/StudentProfile.cshtml");
        }
        
        [HttpGet]
        public ActionResult Enrollment()
        {
            // View is located at Views/Account/LogIn.cshtml
            return View("~/Views/Student/StudentEnroll.cshtml");
        }
        [HttpGet]
        public ActionResult Grade()
        {
            // View is located at Views/Account/LogIn.cshtml
            return View("~/Views/Student/ViewGrades.cshtml");
        }

        [HttpGet]
        public ActionResult Schedule()
        {
            // View is located at Views/Account/LogIn.cshtml
            return View("~/Views/Student/ClassSchedule.cshtml");
        }
        // public ActionResult LoginTeacher()
        // {
        //     return View("~/Views/Account/LoginProfessor.cshtml");
        // }
        //
        // public ActionResult LoginHead()
        // {
        //     return View("~/Views/Account/LoginProgramHead.cshtml");
        //
        // }
        //
        // public ActionResult LoginAdmin()
        // {
        //     return View("~/Views/Account/LoginAdmin.cshtml");
        // }
        //
        // public ActionResult SignUp()
        // {
        //     return View("~/Views/Account/SignUp.cshtml");
        // }
    }
}