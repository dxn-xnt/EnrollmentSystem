using System.Web.Mvc;
using System.Collections.Generic;
using Student_Enrollment_System.Models;
using Npgsql;
namespace Student_Enrollment_System.Controllers
{
    public class AdminController : Controller
    {
        private readonly string _connectionString;

        public AdminController()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString;
        }

        public ActionResult Dashboard()
        {
            List<int> statList = GetDashboardStat();
            ViewBag.statList = statList;
            return View("~/Views/Admin/Dashboard.cshtml");
        }
        
        
        public ActionResult Curriculums()
        {
            var programs = GetProgramsFromDatabase();
            var academicYears = GetAcademicYearsFromDatabase();
            var yearLevels = GetYearLevelFromDatabase();

            ViewBag.AcademicYears = academicYears;
            ViewBag.YearLevels = yearLevels;
            ViewBag.Semesters = GetSemesterFromDatabase();

            return View("~/Views/Admin/Curriculum.cshtml", programs);
        }


        public ActionResult Course()
        {
            // Redirect to CourseController.Index instead of duplicating logic
            return RedirectToAction("Index", "Course");
        }

        public ActionResult Admin_AddCourse()
        {
            return RedirectToAction("Index", "AddProgram"); // Use AddProgramController
        }

        public ActionResult EditCourse()
        {
            return View("~/Views/Admin/EditCourse.cshtml");
        }
        
        private List<Program> GetProgramsFromDatabase()
        {
            var programs = new List<Program>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"prog_code\", \"prog_title\" FROM \"program\"", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            programs.Add(new Program
                            {
                                Code = reader.GetString(0),
                                Title = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return programs;
        }

        private List<AcademicYear> GetAcademicYearsFromDatabase()
        {
            var academicYears = new List<AcademicYear>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"ay_code\", \"ay_start_year\", \"ay_end_year\" FROM \"academic_year\"", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            academicYears.Add(new AcademicYear
                            {
                                Code = reader.GetString(0),
                                StartYear = reader.GetInt16(1),
                                EndYear = reader.GetInt16(2),
                            });
                        }
                    }
                }
            }

            return academicYears;
        }
        
        private List<YearLevel> GetYearLevelFromDatabase()
        {
            var yearLevel = new List<YearLevel>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"yrl_id\", \"yrl_title\" FROM \"year_level\" ", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yearLevel.Add(new YearLevel
                            {
                                Id = reader.GetInt16(0),
                                Title = reader.GetString(1),
                            });
                        }
                    }
                }
            }

            return yearLevel;
        }

        private List<Semester> GetSemesterFromDatabase()
        {
            var semesters = new List<Semester>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"sem_id\", \"sem_name\" FROM \"semester\"", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            semesters.Add(new Semester
                            {
                                Id = reader.GetInt16(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return semesters;
        }
        

        public List<int> GetDashboardStat()
        {
            var statList = new List<int>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                statList.Add(GetStat(conn, "SELECT COUNT(*) FROM student"));
                statList.Add(GetStat(conn, "SELECT COUNT(*) FROM Faculty WHERE FCL_TYPE = 'professor'"));
                statList.Add(GetStat(conn, "SELECT COUNT(*) FROM Faculty WHERE FCL_TYPE = 'admin'"));
                statList.Add(GetStat(conn, "SELECT COUNT(*) FROM Faculty WHERE FCL_TYPE = 'programhead'"));
                statList.Add(GetStat(conn, "SELECT COUNT(*) FROM Course"));
            }
            return statList;
        }

        public int GetStat(NpgsqlConnection conn, string query)
        {
            int stat = 0;
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stat = reader.GetInt32(0);
                    }
                }   
            }
            return stat;
        }
    }
}