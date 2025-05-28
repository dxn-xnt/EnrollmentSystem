using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
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
        
        // Example controller action for saving schedule
        [HttpPost]
        [Route("/Head/Schedules/SaveSchedule")]
        public ActionResult SaveSchedule(ScheduleViewModel model) 
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(model.Schedule.CourseCode) || 
                    string.IsNullOrEmpty(model.Schedule.BlockSectionCode) || 
                    model.Schedule.ProfessorId < 0 || 
                    model.Schedule.RoomId < 0||
                    model.TimeSlot == null || 
                    !model.TimeSlot.Any())
                {
                    return Json(new { 
                        mess = 0, 
                        error = "Course, section, instructor, room, and at least one timeslot are required.",
                        field = "CourseId"
                    });
                }

                using (var db = new NpgsqlConnection(_connectionString))
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            var scheduleId = SaveScheduleToDatabase(db, transaction, model);

                            // Save timeslots
                            foreach (var timeslot in model.TimeSlot)
                            {
                                SaveTimeslotToDatabase(db, transaction, scheduleId, timeslot);
                            }

                            transaction.Commit();
                            
                            return Json(new {
                                mess = 1,
                                message = "Schedule saved successfully",
                                redirectUrl = Url.Action("Schedules", "ProgramHead")
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
        
        [HttpGet]
        [Route("/Head/Schedules/GetSchedule")]
        public ActionResult GetSchedule(string sectionId)
        {
            try
            {
                // Validate required field
                if (string.IsNullOrWhiteSpace(sectionId))
                {
                    return Json(new { 
                        success = false,
                        message = "Validation failed",
                        error = "Section ID is required",
                        field = "sectionId"
                    }, JsonRequestBehavior.AllowGet);
                }

                using (var db = new NpgsqlConnection(_connectionString))
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            // Check if section exists
                            var existsQuery = "SELECT COUNT(*) FROM BLOCK_SECTION WHERE BSEC_CODE = @SectionId";
                            var existsCmd = new NpgsqlCommand(existsQuery, db, transaction);
                            existsCmd.Parameters.AddWithValue("@SectionId", sectionId);
                            
                            if (Convert.ToInt32(existsCmd.ExecuteScalar()) == 0)
                            {
                                return Json(new { 
                                    success = false,
                                    message = "Not found",
                                    error = "Section not found",
                                    field = "sectionId"
                                }, JsonRequestBehavior.AllowGet);
                            }

                            // Get schedule data optimized for frontend rendering
                            var scheduleQuery = @"
                                SELECT 
                                    s.SCHD_ID AS id,
                                    c.CRS_CODE AS courseCode,
                                    c.CRS_TITLE AS courseTitle,
                                    c.CRS_UNITS AS units,
                                    bs.BSEC_NAME AS sectionName,
                                    p.PROF_NAME AS instructorName,
                                    r.ROOM_ID AS roomNumber,
                                    json_agg(
                                        json_build_object(
                                            'day', ts.TSL_DAY,
                                            'startTime', TO_CHAR(ts.TSL_START_TIME, 'HH24:MI'),
                                            'endTime', TO_CHAR(ts.TSL_END_TIME, 'HH24:MI'),
                                            'hasConflict', EXISTS (
                                                SELECT 1 FROM SCHEDULE s2
                                                JOIN TIME_SLOT ts2 ON s2.SCHD_ID = ts2.SCHD_ID
                                                WHERE (s2.PROF_ID = s.PROF_ID OR s2.ROOM_ID = s.ROOM_ID)
                                                AND s2.SCHD_ID != s.SCHD_ID
                                                AND ts2.TSL_DAY = ts.TSL_DAY
                                                AND (
                                                    (ts2.TSL_START_TIME < ts.TSL_END_TIME AND ts2.TSL_END_TIME > ts.TSL_START_TIME)
                                                )
                                            )
                                        ) ORDER BY ts.TSL_DAY, ts.TSL_START_TIME
                                    ) AS timeSlots
                                FROM SCHEDULE s
                                JOIN TIME_SLOT ts ON s.SCHD_ID = ts.SCHD_ID
                                JOIN COURSE c ON s.CRS_CODE = c.CRS_CODE
                                JOIN BLOCK_SECTION bs ON s.BSEC_CODE = bs.BSEC_CODE
                                JOIN PROFESSOR p ON s.PROF_ID = p.PROF_ID
                                JOIN ROOM r ON s.ROOM_ID = r.ROOM_ID
                                WHERE s.BSEC_CODE = @SectionId
                                GROUP BY s.SCHD_ID, c.CRS_CODE, c.CRS_TITLE, c.CRS_UNITS, bs.BSEC_NAME, p.PROF_NAME, r.ROOM_ID
                                ORDER BY MIN(ts.TSL_DAY), MIN(ts.TSL_START_TIME)";

                            var scheduleCmd = new NpgsqlCommand(scheduleQuery, db, transaction);
                            scheduleCmd.Parameters.AddWithValue("@SectionId", sectionId);

                            var schedules = new List<dynamic>();
                            using (var reader = scheduleCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    schedules.Add(new {
                                        id = reader["id"],
                                        courseCode = reader["courseCode"],
                                        courseTitle = reader["courseTitle"],
                                        units = reader["units"],
                                        sectionName = reader["sectionName"],
                                        instructorName = reader["instructorName"],
                                        roomNumber = reader["roomNumber"],
                                        timeSlots = JsonConvert.DeserializeObject<List<dynamic>>(reader["timeSlots"].ToString())
                                    });
                                }
                            }

                            transaction.Commit();

                            if (schedules.Count == 0)
                            {
                                return Json(new { 
                                    success = true,
                                    message = "No schedule found for this section",
                                    data = new List<object>()
                                }, JsonRequestBehavior.AllowGet);
                            }

                            return Json(new {
                                success = true,
                                message = "Schedule loaded successfully",
                                data = schedules
                            }, JsonRequestBehavior.AllowGet);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { 
                                success = false,
                                message = "Database error",
                                error = ex.Message
                            }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false,
                    message = "Server error",
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        private int SaveScheduleToDatabase(NpgsqlConnection db, NpgsqlTransaction transaction, ScheduleViewModel model)
        {
            // Validate input parameters
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (model?.Schedule == null) throw new ArgumentNullException(nameof(model.Schedule));

            // Validate required fields
            if (string.IsNullOrWhiteSpace(model.Schedule.CourseCode))
                throw new ArgumentException("Course code is required", nameof(model.Schedule.CourseCode));
            if (string.IsNullOrWhiteSpace(model.Schedule.BlockSectionCode))
                throw new ArgumentException("Section code is required", nameof(model.Schedule.BlockSectionCode));
            if (model.Schedule.ProfessorId < 0 )
                throw new ArgumentException("Professor ID is required", nameof(model.Schedule.ProfessorId));
            if (model.Schedule.RoomId < 0)
                throw new ArgumentException("Room ID is required", nameof(model.Schedule.RoomId));

            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO SCHEDULE 
                (CRS_CODE, BSEC_CODE, PROF_ID, ROOM_ID)
                VALUES (@CourseCode, @BlockSectionCode, @ProfessorId, @RoomId)
                RETURNING SCHD_ID", db, transaction))
            {
                cmd.Parameters.AddWithValue("@CourseCode", model.Schedule.CourseCode);
                cmd.Parameters.AddWithValue("@BlockSectionCode", model.Schedule.BlockSectionCode);
                cmd.Parameters.AddWithValue("@ProfessorId", model.Schedule.ProfessorId);
                cmd.Parameters.AddWithValue("@RoomId", model.Schedule.RoomId);

                // Execute and handle possible null result
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    throw new InvalidOperationException("Failed to retrieve schedule ID after insertion");
                }

                return Convert.ToInt32(result);
            }
        }

        private void SaveTimeslotToDatabase(NpgsqlConnection db, NpgsqlTransaction transaction, int scheduleId, TimeSlot timeslot)
        {
            var cmd = new NpgsqlCommand(@"
                INSERT INTO TIME_SLOT
                (SCHD_ID, TSL_DAY, TSL_START_TIME, TSL_END_TIME)
                VALUES (@ScheduleId, @Day, @StartTime, @EndTime)", db, transaction);
            
            cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
            cmd.Parameters.AddWithValue("@Day", timeslot.Day);
            cmd.Parameters.AddWithValue("@StartTime", timeslot.StartTime);
            cmd.Parameters.AddWithValue("@EndTime", timeslot.EndTime);

            cmd.ExecuteNonQuery();
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
        
        private Student GetStudentFromDatabase(int studentId)
        {
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
                      FROM student
                      WHERE stud_id = @StudentId", conn))
                {
                    cmd.Parameters.AddWithValue("@StudentId", studentId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Student
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
                            };
                        }
                    }
                }
            }
            return null; // Return null if no student found
        }
    }
}