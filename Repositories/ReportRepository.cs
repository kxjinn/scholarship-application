using Microsoft.Data.SqlClient;
using scholarship_Application.Models;
using scholarship_Application.Repositories.Interfaces;
using scholarship_Application.Utilities;
using System;
using System.Data;

namespace scholarship_Application.Repositories
{
    public class ReportRepository : IReportRepository
    {
        public DataTable GetApplicationSummary(int? scholarshipId, string academicYear)
        {
            string query = @"SELECT s.Name AS [Scholarship], s.Slots AS [Slots],
                            COUNT(a.ApplicationId) AS [Total],
                            SUM(CASE WHEN a.StatusId = @Submitted   THEN 1 ELSE 0 END) AS [Submitted],
                            SUM(CASE WHEN a.StatusId = @UnderReview THEN 1 ELSE 0 END) AS [Under Review],
                            SUM(CASE WHEN a.StatusId = @Approved    THEN 1 ELSE 0 END) AS [Approved],
                            SUM(CASE WHEN a.StatusId = @NotSelected THEN 1 ELSE 0 END) AS [Not Selected]
                            FROM Scholarships s
                            LEFT JOIN Applications a ON a.ScholarshipId = s.ScholarshipId
                                 AND (@AcademicYear IS NULL OR a.AcademicYear = @AcademicYear)
                            WHERE (@ScholarshipId IS NULL OR s.ScholarshipId = @ScholarshipId)
                            GROUP BY s.ScholarshipId, s.Name, s.Slots
                            ORDER BY s.Name";

            return RunReport(query, cmd =>
            {
                AddStatusParameters(cmd);
                AddFilterParameters(cmd, scholarshipId, academicYear);
            });
        }

        public DataTable GetApprovedGrantees(int? scholarshipId, string academicYear)
        {
            string query = @"SELECT ap.StudentNo AS [Student No],
                            ap.LastName + ', ' + ap.FirstName + ISNULL(' ' + ap.MiddleName, '') AS [Name],
                            c.CourseCode AS [Course], ap.YearLevel AS [Year Level],
                            s.Name AS [Scholarship], a.AcademicYear AS [Academic Year],
                            s.AmountPerSem AS [Amount Per Sem],
                            (SELECT MAX(h.ChangedAt) FROM ApplicationStatusHistory h
                             WHERE h.ApplicationId = a.ApplicationId
                             AND h.StatusId = @Approved) AS [Date Approved]
                            FROM Applications a
                            INNER JOIN Applicants ap ON ap.ApplicantId = a.ApplicantId
                            INNER JOIN Courses c ON c.CourseId = ap.CourseId
                            INNER JOIN Scholarships s ON s.ScholarshipId = a.ScholarshipId
                            WHERE a.StatusId = @Approved
                            AND (@ScholarshipId IS NULL OR a.ScholarshipId = @ScholarshipId)
                            AND (@AcademicYear IS NULL OR a.AcademicYear = @AcademicYear)
                            ORDER BY s.Name, ap.LastName, ap.FirstName";

            return RunReport(query, cmd =>
            {
                cmd.AddParameter("@Approved", SqlDbType.Int, (int)ApplicationStatus.Approved);
                AddFilterParameters(cmd, scholarshipId, academicYear);
            });
        }

        public DataTable GetMissingRequirements(int? scholarshipId, string academicYear)
        {
            string query = @"SELECT ap.StudentNo AS [Student No],
                            ap.LastName + ', ' + ap.FirstName + ISNULL(' ' + ap.MiddleName, '') AS [Applicant],
                            c.CourseCode AS [Course], ap.ContactNo AS [Contact No],
                            s.Name AS [Scholarship], a.AcademicYear AS [Academic Year],
                            r.Name AS [Missing Requirement]
                            FROM ApplicationRequirements ar
                            INNER JOIN Applications a ON a.ApplicationId = ar.ApplicationId
                            INNER JOIN Applicants ap ON ap.ApplicantId = a.ApplicantId
                            INNER JOIN Courses c ON c.CourseId = ap.CourseId
                            INNER JOIN Scholarships s ON s.ScholarshipId = a.ScholarshipId
                            INNER JOIN Requirements r ON r.RequirementId = ar.RequirementId
                            WHERE ar.IsSubmitted = 0
                            AND a.StatusId IN (@Submitted, @UnderReview)
                            AND (@ScholarshipId IS NULL OR a.ScholarshipId = @ScholarshipId)
                            AND (@AcademicYear IS NULL OR a.AcademicYear = @AcademicYear)
                            ORDER BY ap.LastName, ap.FirstName, r.Name";

            return RunReport(query, cmd =>
            {
                cmd.AddParameter("@Submitted", SqlDbType.Int, (int)ApplicationStatus.Submitted);
                cmd.AddParameter("@UnderReview", SqlDbType.Int, (int)ApplicationStatus.UnderReview);
                AddFilterParameters(cmd, scholarshipId, academicYear);
            });
        }

        private static void AddStatusParameters(SqlCommand cmd)
        {
            cmd.AddParameter("@Submitted", SqlDbType.Int, (int)ApplicationStatus.Submitted);
            cmd.AddParameter("@UnderReview", SqlDbType.Int, (int)ApplicationStatus.UnderReview);
            cmd.AddParameter("@Approved", SqlDbType.Int, (int)ApplicationStatus.Approved);
            cmd.AddParameter("@NotSelected", SqlDbType.Int, (int)ApplicationStatus.NotSelected);
        }

        private static void AddFilterParameters(SqlCommand cmd, int? scholarshipId, string academicYear)
        {
            cmd.AddParameter("@ScholarshipId", SqlDbType.Int, scholarshipId);
            cmd.AddText("@AcademicYear", 9, academicYear, true);
        }

        private static DataTable RunReport(string query, Action<SqlCommand> addParameters)
        {
            DataTable table = new DataTable();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    addParameters(cmd);

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(table);
                    }
                }
            }

            return table;
        }
    }
}
