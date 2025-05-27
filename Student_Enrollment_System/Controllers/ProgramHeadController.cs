using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Npgsql;
using Student_Enrollment_System.Models;

namespace Student_Enrollment_System.Controllers
{
    public class ProgramHeadController : Controller
    {
        private readonly string _connectionString;

        public ProgramHeadController()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString;
        }
        // GET
        public ActionResult Dashboard()
        {
            return View("~/Views/Program-Head/Dashboard.cshtml");
        }
        
        public ActionResult Students()
        {
            var students = GetStudentsFromDatabase();
            return View("~/Views/Program-Head/StudentManagement.cshtml", students);
        }
        public ActionResult Enrollments()
        {
            return View("~/Views/Program-Head/EnrollmentApproval.cshtml");
        }
        public ActionResult Schedules()
        {
            var courses = GetCoursesFromDatabase();
            ViewBag.BlockSections = GetBlockSectionsFromDatabase();
            ViewBag.Programs = GetProgramsFromDatabase();
            ViewBag.YearLevels = GetYearLevelFromDatabase();
            return View("~/Views/Program-Head/SetSchedules.cshtml", courses);
        }
        public ActionResult StudentManagement()
        {
            return View("~/Views/Program-Head/StudentManagement.cshtml");
        }
        public ActionResult ClassManagement()
        {
            return View("~/Views/Program-Head/ClassManagement.cshtml");
        }
        private List<BlockSection> GetBlockSectionsFromDatabase()
        {
            var blockSections = new List<BlockSection>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                SELECT 
                    BSEC_CODE,
                    BSEC_NAME,
                    BSEC_STATUS,
                    PROG_CODE,
                    AY_CODE,
                    SEM_ID,
                    YRL_ID
                FROM BLOCK_SECTION", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            blockSections.Add(new BlockSection
                            {
                                Code = reader["BSEC_CODE"]?.ToString(),
                                Name = reader["BSEC_NAME"]?.ToString(),
                                Status = reader["BSEC_STATUS"]?.ToString(),
                                ProgramCode = reader["PROG_CODE"]?.ToString(),
                                AcademicYear = reader["AY_CODE"]?.ToString(),
                                Semester = reader["SEM_ID"] != DBNull.Value ? Convert.ToInt32(reader["SEM_ID"]) : 0,
                                YearLevel = reader["YRL_ID"] != DBNull.Value ? Convert.ToInt32(reader["SEM_ID"]) : 0
                            });
                        }
                    }
                }
            }

            return blockSections;
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
        
        private List<Student> GetStudentsFromDatabase()
        {
            var students = new List<Student>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT 
                        stud_id, stud_fname, stud_lname, stud_mname, 
                        stud_email, stud_dob, stud_contact, stud_city_address, 
                        stud_home_address, stud_district, stud_is_first_gen, 
                        stud_yr_level, stud_major, stud_status, stud_sem, 
                        bsec_code, prog_code
                      FROM student", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            students.Add(new Student
                            {
                                Id = reader.GetInt32(0),
                                FirstName = reader.GetString(1),
                                LastName = reader.GetString(2),
                                MiddleName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Email = reader.GetString(4),
                                Birthdate = reader.GetDateTime(5),
                                Contact = reader.GetString(6),
                                CityAddress = reader.IsDBNull(7) ? null : reader.GetString(7),
                                HomeAddress = reader.IsDBNull(8) ? null : reader.GetString(8),
                                District = reader.IsDBNull(9) ? null : reader.GetString(9),
                                IsFirstGen = reader.GetBoolean(10),
                                YearLevel = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                                Major = reader.IsDBNull(12) ? null : reader.GetString(12),
                                Status = reader.IsDBNull(13) ? null : reader.GetString(13),
                                Semester = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                                BlockSection = reader.IsDBNull(15) ? null : reader.GetString(15),
                                ProgramCode = reader.IsDBNull(16) ? null : reader.GetString(16)
                            });
                        }
                    }
                }
            }

            return students;
        }
    }
}