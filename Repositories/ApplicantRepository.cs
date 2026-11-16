using Microsoft.Data.SqlClient;
using scholarship_Application.Models;
using scholarship_Application.Repositories.Interfaces;
using scholarship_Application.Utilities;
using System;
using System.Collections.Generic;
using System.Data;

namespace scholarship_Application.Repositories
{
    public class ApplicantRepository : IApplicantRepository
    {
        private const string SelectApplicants = @"
            SELECT a.ApplicantId, a.StudentNo, a.FirstName, a.MiddleName, a.LastName,
                   a.BirthDate, a.Gender, a.CourseId, c.CourseCode, c.CourseName, a.YearLevel,
                   a.ContactNo, a.Email, a.Address, a.IsActive, a.CreatedAt
            FROM Applicants a
            INNER JOIN Courses c ON c.CourseId = a.CourseId";

        public int AddApplicant(Applicant applicant)
        {
            string query = @"INSERT INTO Applicants
                            (StudentNo, FirstName, MiddleName, LastName, BirthDate, Gender,
                             CourseId, YearLevel, ContactNo, Email, Address, IsActive)
                            OUTPUT INSERTED.ApplicantId
                            VALUES
                            (@StudentNo, @FirstName, @MiddleName, @LastName, @BirthDate, @Gender,
                             @CourseId, @YearLevel, @ContactNo, @Email, @Address, 1)";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    AddApplicantParameters(cmd, applicant);

                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public void UpdateApplicant(Applicant applicant)
        {
            string query = @"UPDATE Applicants SET StudentNo = @StudentNo, FirstName = @FirstName,
                            MiddleName = @MiddleName, LastName = @LastName, BirthDate = @BirthDate,
                            Gender = @Gender, CourseId = @CourseId, YearLevel = @YearLevel,
                            ContactNo = @ContactNo, Email = @Email, Address = @Address
                            WHERE ApplicantId = @ApplicantId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    AddApplicantParameters(cmd, applicant);
                    cmd.AddParameter("@ApplicantId", SqlDbType.Int, applicant.ApplicantId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetApplicantActive(int applicantId, bool isActive)
        {
            string query = @"UPDATE Applicants SET IsActive = @IsActive WHERE ApplicantId = @ApplicantId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@IsActive", SqlDbType.Bit, isActive);
                    cmd.AddParameter("@ApplicantId", SqlDbType.Int, applicantId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Applicant> Search(string keyword, bool activeOnly)
        {
            string query = SelectApplicants + @"
                            WHERE (@ActiveOnly = 0 OR a.IsActive = 1)
                            AND (a.StudentNo LIKE @Like
                                 OR a.FirstName LIKE @Like
                                 OR a.LastName LIKE @Like
                                 OR a.FirstName + ' ' + a.LastName LIKE @Like)
                            ORDER BY a.LastName, a.FirstName";

            List<Applicant> applicants = new List<Applicant>();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ActiveOnly", SqlDbType.Bit, activeOnly);
                    cmd.AddText("@Like", 110, "%" + (keyword ?? "").Trim() + "%");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            applicants.Add(MapApplicant(reader));
                        }
                    }
                }
            }

            return applicants;
        }

        public Applicant GetById(int id)
        {
            string query = SelectApplicants + " WHERE a.ApplicantId = @ApplicantId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ApplicantId", SqlDbType.Int, id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null; // Applicant not found
                        }

                        return MapApplicant(reader);
                    }
                }
            }
        }

        public bool StudentNoExists(string studentNo, int excludeApplicantId = 0)
        {
            string query = @"SELECT COUNT(*) FROM Applicants
                            WHERE StudentNo = @StudentNo AND ApplicantId <> @ExcludeApplicantId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddText("@StudentNo", 20, studentNo);
                    cmd.AddParameter("@ExcludeApplicantId", SqlDbType.Int, excludeApplicantId);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        // Helper method for inserting/adding and updating applicants to avoid code duplication
        private static void AddApplicantParameters(SqlCommand cmd, Applicant applicant)
        {
            cmd.AddText("@StudentNo", 20, applicant.StudentNo);
            cmd.AddText("@FirstName", 60, applicant.FirstName);
            cmd.AddText("@MiddleName", 60, applicant.MiddleName, true);
            cmd.AddText("@LastName", 60, applicant.LastName);
            cmd.AddParameter("@BirthDate", SqlDbType.Date, applicant.BirthDate.Date);
            cmd.AddText("@Gender", 20, applicant.Gender);
            cmd.AddParameter("@CourseId", SqlDbType.Int, applicant.CourseId);
            cmd.AddParameter("@YearLevel", SqlDbType.TinyInt, (byte)applicant.YearLevel);
            cmd.AddText("@ContactNo", 20, applicant.ContactNo);
            cmd.AddText("@Email", 100, applicant.Email);
            cmd.AddText("@Address", 100, applicant.Address);
        }

        private static Applicant MapApplicant(SqlDataReader reader)
        {
            return new Applicant
            {
                ApplicantId = Convert.ToInt32(reader["ApplicantId"]),
                StudentNo = reader["StudentNo"].ToString(),
                FirstName = reader["FirstName"].ToString(),
                MiddleName = reader["MiddleName"] as string,
                LastName = reader["LastName"].ToString(),
                BirthDate = Convert.ToDateTime(reader["BirthDate"]),
                Gender = reader["Gender"].ToString(),
                CourseId = Convert.ToInt32(reader["CourseId"]),
                CourseCode = reader["CourseCode"].ToString(),
                CourseName = reader["CourseName"].ToString(),
                YearLevel = Convert.ToInt32(reader["YearLevel"]),
                ContactNo = reader["ContactNo"].ToString(),
                Email = reader["Email"].ToString(),
                Address = reader["Address"].ToString(),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }
    }
}
