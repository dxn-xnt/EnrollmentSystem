using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Student_Enrollment_System.Models;
using Npgsql;

namespace Student_Enrollment_System.Controllers
{
    public class AddCourseController : Controller
    {
        private readonly string _connectionString;

        public AddCourseController()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString;
        }

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.CourseCategories = GetCourseCategoriesFromDatabase();
            ViewBag.CoursesForPrereq = GetCoursesFromDatabase();
            return View("~/Views/Admin/AddCourse.cshtml");
        }

        [HttpPost]
        [Route("/Admin/Course/AddCourse")]
        public ActionResult Index(Course course) 
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(course.Code) || 
                    string.IsNullOrEmpty(course.Title) ||
                    course.Units <= 0)
                {
                    return Json(new { 
                        mess = 0, 
                        error = "Course code, title, and units are required fields.",
                        field = "Crs_Code"
                    });
                }

                using (var db = new NpgsqlConnection(_connectionString))
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            // Check if course exists
                            var existsCmd = new NpgsqlCommand(
                                "SELECT COUNT(*) FROM COURSE WHERE CRS_CODE = @Code", db, transaction);
                            existsCmd.Parameters.AddWithValue("@Code", course.Code);
                            if (Convert.ToInt32(existsCmd.ExecuteScalar()) > 0)
                            {
                                return Json(new { 
                                    mess = 2, 
                                    error = "Course already exists",
                                    field = "Crs_Code"
                                });
                            }

                            // Insert course - USING DATABASE COLUMN NAMES
                            var insertCmd = new NpgsqlCommand(@"
                                INSERT INTO COURSE 
                                (CRS_CODE, CRS_TITLE, CRS_UNITS, CRS_LEC, CRS_LAB, CTG_CODE)
                                VALUES (@Code, @Title, @Units, @LecHours, @LabHours, @CategoryCode)
                                RETURNING CRS_CODE", db, transaction);
                            
                            insertCmd.Parameters.AddWithValue("@Code", course.Code);
                            insertCmd.Parameters.AddWithValue("@Title", course.Title);
                            insertCmd.Parameters.AddWithValue("@Units", course.Units);
                            insertCmd.Parameters.AddWithValue("@LecHours", course.LecHours);
                            insertCmd.Parameters.AddWithValue("@LabHours", course.LabHours);
                            insertCmd.Parameters.AddWithValue("@CategoryCode", course.CategoryCode);

                            var courseCode = (string)insertCmd.ExecuteScalar();

                            if (course.Prerequisites != null)
                            {
                                foreach (var prereq in course.Prerequisites)
                                {
                                    var preqCode = prereq?.ToString()?.Trim();
        
                                    if (!string.IsNullOrWhiteSpace(preqCode))
                                    {
                                        using (var prereqCmd = new NpgsqlCommand(
                                                   "INSERT INTO PREREQUISITE (CRS_CODE, PREQ_CRS_CODE) " +
                                                   "VALUES (@courseCode, @prereqCode)", db, transaction))
                                        {
                                            prereqCmd.Parameters.AddWithValue("@courseCode", courseCode);
                                            prereqCmd.Parameters.AddWithValue("@prereqCode", preqCode);
                                            prereqCmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }

                            transaction.Commit();
                            return Json(new {
                                mess = 1,
                                message = "Course created successfully",
                                redirectUrl = Url.Action("Index", "Course")
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { 
                                mess = 0, 
                                error = ex.Message 
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { 
                    mess = 0, 
                    error = ex.Message 
                });
            }
        }
                
        private List<CourseCategory> GetCourseCategoriesFromDatabase()
        {
            var categories = new List<CourseCategory>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"ctg_code\", \"ctg_name\" FROM \"course_category\"", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new CourseCategory
                            {
                                Code = reader.GetString(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return categories;
        }
        
        private List<Course> GetCoursesFromDatabase()
        {
            var courses = new List<Course>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"crs_code\", \"crs_title\", \"crs_units\", \"crs_lec\", \"crs_lab\", \"ctg_code\" FROM \"course\"", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courses.Add(new Course
                            {
                                Code = reader.GetString(0),
                                Title = reader.GetString(1),
                                Units = reader.GetInt32(2),
                                LecHours = reader.GetInt32(3),
                                LabHours = reader.GetInt32(4),
                                CategoryCode = reader.GetString(5)
                            });
                        }
                    }
                }
            }

            return courses;
        }
        
        
    }
}