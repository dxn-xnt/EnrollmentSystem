using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Student_Enrollment_System.Models;
using Npgsql;

namespace Student_Enrollment_System.Controllers
{
    public class CurriculumController : Controller
    {
        
        private readonly string _connectionString;

        public CurriculumController() 
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString;
        }

        public ActionResult Index()
        {
            var programs = GetProgramsFromDatabase();
            ViewBag.AcademicYears = GetAcademicYearsFromDatabase();
            ViewBag.YearLevels = GetYearLevelFromDatabase();
            ViewBag.Semesters = GetSemesterFromDatabase();
            ViewBag.Prerequisites = GetPrerequisitesFromDatabase();
            ViewBag.Courses = GetCoursesFromDatabase();
            ViewBag.Curriculum = GetCurriculumFromDatabase();
            ViewBag.CurriculumCourses = GetCurriculumCoursesFromDatabase();
            return View("~/Views/Admin/Curriculum.cshtml", programs);
        }

        
        [HttpPost]
        [Route("/Admin/Curriculum/AssignCourses")]
        public ActionResult AssignCourses(CurriculumAssignmentRequest data)
        {
            try
            {
                // Validate required fields
                if (data == null || data.Curriculum == null || data.CurriculumCourses == null)
                {
                    return Json(new { 
                        mess = 0, 
                        error = "Invalid data format",
                        field = "general"
                    });
                }

                using (var db = new NpgsqlConnection(_connectionString))
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(data.Curriculum.Code))
                            {
                                return Json(new { 
                                    mess = 0, 
                                    error = "Curriculum code is required",
                                    field = "Curriculum Code"
                                });
                            }

                            // Handle Curriculum
                            var curriculumCheckCmd = new NpgsqlCommand(
                                "SELECT COUNT(*) FROM CURRICULUM WHERE CUR_CODE = @curCode AND PROG_CODE = @progCode AND AY_CODE = @ayCode", 
                                db, transaction);
                            
                            curriculumCheckCmd.Parameters.AddWithValue("@curCode", data.Curriculum.Code);
                            curriculumCheckCmd.Parameters.AddWithValue("@progCode", data.Curriculum.ProgramCode);
                            curriculumCheckCmd.Parameters.AddWithValue("@ayCode", data.Curriculum.AcademicYear);
                            
                            if (Convert.ToInt32(curriculumCheckCmd.ExecuteScalar()) == 0)
                            {
                                var insertCurriculumCmd = new NpgsqlCommand(
                                    "INSERT INTO CURRICULUM (CUR_CODE, PROG_CODE, AY_CODE) " +
                                    "VALUES (@curCode, @progCode, @ayCode)", 
                                    db, transaction);
                                
                                insertCurriculumCmd.Parameters.AddWithValue("@curCode", data.Curriculum.Code);
                                insertCurriculumCmd.Parameters.AddWithValue("@progCode", data.Curriculum.ProgramCode);
                                insertCurriculumCmd.Parameters.AddWithValue("@ayCode", data.Curriculum.AcademicYear);
                                
                                insertCurriculumCmd.ExecuteNonQuery();
                            }

                            // Check for duplicate courses in the request
                            var duplicateCourses = data.CurriculumCourses
                                .GroupBy(c => new { c.CourseCode, c.YearLevel, c.Semester })
                                .Where(g => g.Count() > 1)
                                .Select(g => g.Key.CourseCode)
                                .ToList();

                            if (duplicateCourses.Any())
                            {
                                return Json(new { 
                                    mess = 0, 
                                    error = $"Duplicate courses found in request: {string.Join(", ", duplicateCourses)}",
                                    field = "crsCode"
                                });
                            }

                            // Handle Curriculum Courses
                            foreach (var course in data.CurriculumCourses)
                            {
                                // Check if course already exists in curriculum
                                var courseCheckCmd = new NpgsqlCommand(
                                    "SELECT COUNT(*) FROM CURRICULUM_COURSE " +
                                    "WHERE CUR_CODE = @curCode AND CRS_CODE = @crsCode " +
                                    "AND CUR_YEAR_LEVEL = @yearLevel AND CUR_SEMESTER = @semester", 
                                    db, transaction);
                                
                                courseCheckCmd.Parameters.AddWithValue("@curCode", course.CurriculumCode);
                                courseCheckCmd.Parameters.AddWithValue("@crsCode", course.CourseCode);
                                courseCheckCmd.Parameters.AddWithValue("@yearLevel", course.YearLevel);
                                courseCheckCmd.Parameters.AddWithValue("@semester", course.Semester);
                                
                                if (Convert.ToInt32(courseCheckCmd.ExecuteScalar()) > 0)
                                {
                                    transaction.Rollback();
                                    return Json(new { 
                                        mess = 2, 
                                        error = $"Course {course.CourseCode} is already assigned to this curriculum",
                                        field = "crsCode"
                                    });
                                }

                                // Check prerequisites
                                    var prereqCheckCmd = new NpgsqlCommand(
                                        "SELECT p.PREQ_CRS_CODE FROM PREREQUISITE p " +
                                        "WHERE p.CRS_CODE = @crsCode", 
                                        db, transaction);
                                    prereqCheckCmd.Parameters.AddWithValue("@crsCode", course.CourseCode);

                                    // First get all prerequisites for this course
                                    List<string> prerequisites = new List<string>();
                                    using (var prereqReader = prereqCheckCmd.ExecuteReader())
                                    {
                                        while (prereqReader.Read())
                                        {
                                            prerequisites.Add(prereqReader.GetString(0));
                                        }
                                    }

                                    // Then check each prerequisite's position
                                    foreach (string prereqCode in prerequisites)
                                    {
                                        // First check existing curriculum courses
                                        var prereqPositionCmd = new NpgsqlCommand(
                                            "SELECT CUR_YEAR_LEVEL, CUR_SEMESTER FROM CURRICULUM_COURSE " +
                                            "WHERE CUR_CODE = @curCode AND CRS_CODE = @prereqCode", 
                                            db, transaction);
                                        prereqPositionCmd.Parameters.AddWithValue("@curCode", course.CurriculumCode);
                                        prereqPositionCmd.Parameters.AddWithValue("@prereqCode", prereqCode);
                                        
                                        bool prereqFound = false;
                                        int prereqYear = 0;
                                        int prereqSemester = 0;
                                        
                                        using (var positionReader = prereqPositionCmd.ExecuteReader())
                                        {
                                            if (positionReader.Read())
                                            {
                                                prereqFound = true;
                                                prereqYear = positionReader.GetInt32(0);
                                                prereqSemester = positionReader.GetInt32(1);
                                            }
                                        }
                                        
                                        // If not found in existing courses, check temp table
                                        if (!prereqFound)
                                        {
                                            var prereqTempCmd = new NpgsqlCommand(
                                                "SELECT CUR_YEAR_LEVEL, CUR_SEMESTER FROM CURRICULUM_COURSE_TEMP " +
                                                "WHERE CUR_CODE = @curCode AND CRS_CODE = @prereqCode", 
                                                db, transaction);
                                            prereqTempCmd.Parameters.AddWithValue("@curCode", course.CurriculumCode);
                                            prereqTempCmd.Parameters.AddWithValue("@prereqCode", prereqCode);
                                            
                                            using (var tempReader = prereqTempCmd.ExecuteReader())
                                            {
                                                if (tempReader.Read())
                                                {
                                                    prereqFound = true;
                                                    prereqYear = tempReader.GetInt32(0);
                                                    prereqSemester = tempReader.GetInt32(1);
                                                }
                                            }
                                        }
                                        
                                        if (!prereqFound)
                                        {
                                            transaction.Rollback();
                                            return Json(new { 
                                                mess = 0, 
                                                error = $"Prerequisite {prereqCode} for course {course.CourseCode} is missing",
                                                field = "crsCode"
                                            });
                                        }
                                        
                                        if (prereqYear > course.YearLevel || 
                                            (prereqYear == course.YearLevel && prereqSemester >= course.Semester))
                                        {
                                            transaction.Rollback();
                                            return Json(new { 
                                                mess = 0, 
                                                error = $"Prerequisite {prereqCode} for course {course.CourseCode} must come before the course",
                                                field = "crsCode"
                                            });
                                        }
                                    }

                                // Insert into temporary table for validation
                                var tempInsertCmd = new NpgsqlCommand(
                                    "INSERT INTO CURRICULUM_COURSE_TEMP " +
                                    "(CUR_CODE, CRS_CODE, CUR_YEAR_LEVEL, CUR_SEMESTER) " +
                                    "VALUES (@curCode, @crsCode, @yearLevel, @semester)", 
                                    db, transaction);
                                
                                tempInsertCmd.Parameters.AddWithValue("@curCode", course.CurriculumCode);
                                tempInsertCmd.Parameters.AddWithValue("@crsCode", course.CourseCode);
                                tempInsertCmd.Parameters.AddWithValue("@yearLevel", course.YearLevel);
                                tempInsertCmd.Parameters.AddWithValue("@semester", course.Semester);
                                
                                tempInsertCmd.ExecuteNonQuery();
                            }

                            // After all validation passes, insert into the actual table
                            foreach (var course in data.CurriculumCourses)
                            {
                                var insertCourseCmd = new NpgsqlCommand(
                                    "INSERT INTO CURRICULUM_COURSE " +
                                    "(CUR_CODE, CRS_CODE, CUR_YEAR_LEVEL, CUR_SEMESTER) " +
                                    "VALUES (@curCode, @crsCode, @yearLevel, @semester)", 
                                    db, transaction);
                                
                                insertCourseCmd.Parameters.AddWithValue("@curCode", course.CurriculumCode);
                                insertCourseCmd.Parameters.AddWithValue("@crsCode", course.CourseCode);
                                insertCourseCmd.Parameters.AddWithValue("@yearLevel", course.YearLevel);
                                insertCourseCmd.Parameters.AddWithValue("@semester", course.Semester);
                                
                                insertCourseCmd.ExecuteNonQuery();
                            }

                            // Clear the temporary table
                            var clearTempCmd = new NpgsqlCommand(
                                "DELETE FROM CURRICULUM_COURSE_TEMP WHERE CUR_CODE = @curCode", 
                                db, transaction);
                            clearTempCmd.Parameters.AddWithValue("@curCode", data.Curriculum.Code);
                            clearTempCmd.ExecuteNonQuery();

                            transaction.Commit();
                            return Json(new {
                                mess = 1,
                                message = "Courses assigned successfully",
                                redirectUrl = Url.Action("Index", "Curriculum")
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { 
                                mess = 0, 
                                error = ex.Message,
                                field = "general"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { 
                    mess = 0, 
                    error = ex.Message,
                    field = "general"
                });
            }
        }
                      
        public ActionResult Edit(string id)
        {
            return View("~/Views/Admin/Curriculum.cshtml");
        }
        
        [HttpGet]
        [Route("/Admin/Curriculum/GetCurriculumCourses")]
        public ActionResult GetCurriculumCourses(string curriculumCode, int semester, int yearLevel)
        {
            try 
            {
                var courses = GetCurriculumCoursesFromDatabase(curriculumCode, semester, yearLevel);
                return Json(courses, JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
            {
                // Log error
                return new HttpStatusCodeResult(500, "Error retrieving courses");
            }
        }
        
        private List<Course> GetCurriculumCoursesFromDatabase(string curriculumCode, int semester, int yearLevel)
        {
            var curriculumCourses = new List<Course>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT 
                        c.crs_code, 
                        c.crs_title, 
                        c.crs_units, 
                        c.crs_lec, 
                        c.crs_lab,
                        c.ctg_code,
                        (
                            SELECT array_agg(p.preq_crs_code)
                            FROM prerequisite p
                            WHERE p.crs_code = c.crs_code
                        ) AS prerequisites
                    FROM curriculum_course cc
                    JOIN course c ON cc.crs_code = c.crs_code
                    WHERE cc.cur_code = @curriculumCode
                      AND cc.cur_semester = @semester
                      AND cc.cur_year_level = @yearLevel", conn))
                {
                    cmd.Parameters.AddWithValue("@curriculumCode", curriculumCode);
                    cmd.Parameters.AddWithValue("@semester", semester);
                    cmd.Parameters.AddWithValue("@yearLevel", yearLevel);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Safely get prerequisites as string[]
                            string[] prerequisites = Array.Empty<string>();
                            if (!reader.IsDBNull(6)) // Index 6 is the prerequisites column
                            {
                                var dbValue = reader.GetValue(6);
                                if (dbValue is Array pgArray)
                                {
                                    prerequisites = new string[pgArray.Length];
                                    for (int i = 0; i < pgArray.Length; i++)
                                    {
                                        prerequisites[i] = pgArray.GetValue(i)?.ToString() ?? string.Empty;
                                    }
                                }
                            }

                            curriculumCourses.Add(new Course
                            {
                                Code = reader.GetString(0),
                                Title = reader.GetString(1),
                                Units = reader.GetInt32(2),
                                LecHours = reader.GetInt32(3),
                                LabHours = reader.GetInt32(4),
                                CategoryCode = reader.GetString(5),
                                Prerequisites = prerequisites
                            });
                        }
                    }
                }
            }

            return curriculumCourses;
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
        
        private List<Curriculum> GetCurriculumFromDatabase()
        {
            var curricula = new List<Curriculum>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"cur_code\", \"prog_code\", \"ay_code\" FROM \"curriculum\"", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            curricula.Add(new Curriculum
                            {
                                Code = reader.GetString(0),
                                ProgramCode = reader.GetString(1),
                                AcademicYear = reader.GetString(2),
                            });
                        }
                    }
                }
            }

            return curricula;
        }
        
        private List<CurriculumCourse> GetCurriculumCoursesFromDatabase()
        {
            var curriculumCourses = new List<CurriculumCourse>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"cur_code\", \"crs_code\", \"cur_year_level\", \"cur_semester\" FROM \"curriculum_course\"", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            curriculumCourses.Add(new CurriculumCourse
                            {
                                CurriculumCode = reader.GetString(0),
                                CourseCode = reader.GetString(1),
                                YearLevel = reader.GetInt16(2),
                                Semester = reader.GetInt16(3)
                            });
                        }
                    }
                }
            }

            return curriculumCourses;
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
    }
}