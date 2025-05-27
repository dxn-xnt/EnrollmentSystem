using System.Web.Mvc;
using System.Web.Routing;

namespace Student_Enrollment_System
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            
            //Program Head Routes
            routes.MapRoute(
                name: "ProgramHeadClassManagementRoute",
                url: "Head/ManageClass",
                defaults: new { controller = "ProgramHead", action = "ClassManagement" }
            );
            routes.MapRoute(
                name: "ProgramHeadStudentManagementRoute",
                url: "Head/ManageStudent",
                defaults: new { controller = "ProgramHead", action = "StudentManagement" }
            );
            routes.MapRoute(
                name: "ProgramHeadSetScheduleListRoute",
                url: "Head/Schedules",
                defaults: new { controller = "ProgramHead", action = "Schedule" }
            );
            routes.MapRoute(
                name: "ProgramHeadEnrollmentApprovalListRoute",
                url: "Head/EnrollmentApproval",
                defaults: new { controller = "ProgramHead", action = "Approval" }
            );
            routes.MapRoute(
                name: "ProgramHeadViewStudentListRoute",
                url: "Head/Students",
                defaults: new { controller = "ProgramHead", action = "Students" }
            );
            routes.MapRoute(
                name: "ProgramHeadDashboardRoute",
                url: "Head/Dashboard",
                defaults: new { controller = "ProgramHead", action = "Dashboard" }
            );
            
            //Professor Routes
            routes.MapRoute(
                name: "ProfessorClassesRoute",
                url: "Professor/Classes",
                defaults: new { controller = "Professor", action = "Classes" }
            ); 
            routes.MapRoute(
                name: "MainProfessorRoute",
                url: "Professor/Dashboard",
                defaults: new { controller = "Professor", action = "Dashboard" }
            );
            
            //Admin Routes
            routes.MapRoute(
                name: "AdminEditCourseRoute",
                url: "Admin/Course/EditCourse",
                defaults: new { controller = "Course", action = "Delete" }
            );
            routes.MapRoute(
                name: "MainAdminRoute",
                url: "Admin/Dashboard",
                defaults: new { controller = "Admin", action = "Dashboard" }
            );
            
            //Course Routes
            routes.MapRoute(
                name: "AdminAddCourseRoute",
                url: "Admin/Course/AddCourse",
                defaults: new { controller = "AddCourse", action = "Index" }
            );
            routes.MapRoute(
                name: "AdminCourseRoute",
                url: "Admin/Course",
                defaults: new { controller = "Course", action = "Index" }
            ); 
            
            //Curriculum Routes
            routes.MapRoute(
                name: "AdminCurriculumRoute",
                url: "Admin/Curriculum",
                defaults: new { controller = "Curriculum", action = "Index" }
            ); 
            routes.MapRoute(
                name: "AdminAssignCourseRoute",
                url: "Admin/Curriculum/AssignCourses",
                defaults: new { controller = "Curriculum", action = "AssignCourses" }
            ); 
            routes.MapRoute(
                name: "AdminGetCourseRoute",
                url: "Admin/Curriculum/GetCurriculumCourses",
                defaults: new { controller = "Curriculum", action = "GetCurriculumCourses" }
            ); 
            
            
            //Student Routes
            routes.MapRoute(
                name: "StudentScheduleRoute",
                url: "Student/Schedule",
                defaults: new { controller = "Student", action = "Schedule" }
            );
            routes.MapRoute(
                name: "StudentViewGradeRoute",
                url: "Student/Grades",
                defaults: new { controller = "Student", action = "Grade" }
            );
            routes.MapRoute(
                name: "StudentEnrollmentRoute",
                url: "Student/Enrollment",
                defaults: new { controller = "Student", action = "Enrollment" }
            );
            routes.MapRoute(
                name: "StudentProfileRoute",
                url: "Student/Profile",
                defaults: new { controller = "Student", action = "ProfileView" }
            );
            routes.MapRoute(
                name: "StudentRoute",
                url: "Student/Dashboard",
                defaults: new { controller = "Student", action = "Dashboard" }
            );
            
            //Signin Routes
            routes.MapRoute(
                name: "SignUpRoute",
                url: "Account/SignUp",
                defaults: new { controller = "Account", action = "SignUp" }
            );
            
            //Login Routes
            routes.MapRoute(
                name: "FacultyLoginRoute",
                url: "Account/Faculty/Login",
                defaults: new { controller = "Account", action = "FacultyLogIn" }
            );
            routes.MapRoute(
                name: "LoginRoute",
                url: "Account/Student/LogIn",
                defaults: new { controller = "Account", action = "StudentLogIn" }
            );
            
            //Default
            routes.MapRoute(
                name: "Default",
                url: "",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}