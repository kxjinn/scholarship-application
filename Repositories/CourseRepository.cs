using Microsoft.Data.SqlClient;
using scholarship_Application.Models;
using scholarship_Application.Repositories.Interfaces;
using scholarship_Application.Utilities;
using System;
using System.Collections.Generic;
using System.Data;

namespace scholarship_Application.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private const string SelectColumns =
            @"SELECT CourseId, CourseCode, CourseName FROM Courses";

        public int AddCourse(Course course)
        {
            string query = @"INSERT INTO Courses (CourseCode, CourseName)
                            OUTPUT INSERTED.CourseId
                            VALUES (@CourseCode, @CourseName)";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddText("@CourseCode", 20, course.CourseCode);
                    cmd.AddText("@CourseName", 100, course.CourseName);

                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public void UpdateCourse(Course course)
        {
            string query = @"UPDATE Courses SET CourseCode = @CourseCode, CourseName = @CourseName
                            WHERE CourseId = @CourseId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddText("@CourseCode", 20, course.CourseCode);
                    cmd.AddText("@CourseName", 100, course.CourseName);
                    cmd.AddParameter("@CourseId", SqlDbType.Int, course.CourseId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteCourse(int courseId)
        {
            string query = @"DELETE FROM Courses WHERE CourseId = @CourseId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@CourseId", SqlDbType.Int, courseId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Course> GetAll()
        {
            string query = SelectColumns + " ORDER BY CourseCode";

            List<Course> courses = new List<Course>();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courses.Add(MapCourse(reader));
                        }
                    }
                }
            }

            return courses;
        }

        public Course GetById(int id)
        {
            string query = SelectColumns + " WHERE CourseId = @CourseId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@CourseId", SqlDbType.Int, id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null; // Course not found
                        }

                        return MapCourse(reader);
                    }
                }
            }
        }

        public bool CodeExists(string courseCode, int excludeCourseId = 0)
        {
            string query = @"SELECT COUNT(*) FROM Courses
                            WHERE CourseCode = @CourseCode AND CourseId <> @ExcludeCourseId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddText("@CourseCode", 20, courseCode);
                    cmd.AddParameter("@ExcludeCourseId", SqlDbType.Int, excludeCourseId);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public bool IsCourseInUse(int courseId)
        {
            string query = @"SELECT COUNT(*) FROM Applicants WHERE CourseId = @CourseId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@CourseId", SqlDbType.Int, courseId);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        private static Course MapCourse(SqlDataReader reader)
        {
            return new Course
            {
                CourseId = Convert.ToInt32(reader["CourseId"]),
                CourseCode = reader["CourseCode"].ToString(),
                CourseName = reader["CourseName"].ToString()
            };
        }
    }
}
