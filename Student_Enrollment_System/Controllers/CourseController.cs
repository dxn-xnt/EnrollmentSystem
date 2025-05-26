using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using Student_Enrollment_System.Models;
using Npgsql;

namespace Student_Enrollment_System.Controllers
{
    public class CourseController : Controller
    {
        private readonly string _connectionString;

        public CourseController()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString;
        }

        public ActionResult Index()
        {
            var courses = GetCoursesFromDatabase();
            ViewBag.Prerequisites = GetPrerequisitesFromDatabase();
            return View("~/Views/Admin/Courses.cshtml",courses);
        }
      
        // GET: /Course/Edit/{id}
        public ActionResult Edit(string id)
        {
            var course = GetCourseById(id);
            if (course == null) return HttpNotFound();

            return View("~/Views/Admin/EditCourse.cshtml", course);
        }

        // GET: /Course/GetAll
        [HttpGet]
        public JsonResult GetAllCourses()
        {
            var courses = new List<dynamic>();
            try
            {
                using (var db = new NpgsqlConnection(_connectionString))
                {
                    db.Open();
                    using (var cmd = new NpgsqlCommand("SELECT CRS_CODE, CRS_TITLE FROM COURSE", db))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                courses.Add(new
                                {
                                    code = reader["CRS_CODE"].ToString(),
                                    title = reader["CRS_TITLE"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception
                Console.WriteLine($"Error fetching courses: {ex.Message}");
            }

            return Json(courses, JsonRequestBehavior.AllowGet);
        }
        
        private List<Prerequisite> GetPrerequisitesFromDatabase()
        {
            var prerequisites = new List<Prerequisite>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"crs_code\", \"preq_crs_code\" FROM \"prerequisite\"", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            prerequisites.Add(new Prerequisite
                            {
                                CourseCode = reader.GetString(0),
                                PrerequisiteCourseCode = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return prerequisites;
        }
        
        private List<Course> GetCoursesFromDatabase()
        {
            var courses = new List<Course>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
            SELECT 
                c.CRS_CODE, 
                c.CRS_TITLE, 
                COALESCE(cat.CTG_NAME, 'General') AS Category,
                c.CRS_UNITS, 
                c.CRS_LEC, 
                c.CRS_LAB
            FROM COURSE c
            LEFT JOIN COURSE_CATEGORY cat ON c.CTG_CODE = cat.CTG_CODE
            LEFT JOIN PREREQUISITE p ON p.CRS_CODE = c.CRS_CODE", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courses.Add(new Course
                            {
                                Code = reader["CRS_CODE"]?.ToString(),
                                Title = reader["CRS_TITLE"]?.ToString(),
                                CategoryName = reader["Category"]?.ToString(),
                                Units = reader["CRS_UNITS"] != DBNull.Value ? Convert.ToInt32(reader["CRS_UNITS"]) : 0,
                                LecHours = reader["CRS_LEC"] != DBNull.Value ? Convert.ToInt32(reader["CRS_LEC"]) : 0,
                                LabHours = reader["CRS_LAB"] != DBNull.Value ? Convert.ToInt32(reader["CRS_LAB"]) : 0
                            });
                        }
                    }
                }
            }

            return courses;
        }

        private object GetCourseById(string id)
        {
            return null;
        }
    }
}