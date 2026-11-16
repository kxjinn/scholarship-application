using Microsoft.Data.SqlClient;
using scholarship_Application.Models;
using scholarship_Application.Repositories.Interfaces;
using scholarship_Application.Utilities;
using System;
using System.Collections.Generic;
using System.Data;

namespace scholarship_Application.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private const string SelectApplications = @"
            SELECT a.ApplicationId, a.ApplicantId,
                   ap.LastName + ', ' + ap.FirstName AS ApplicantName, ap.StudentNo,
                   c.CourseCode,
                   a.ScholarshipId, s.Name AS ScholarshipName,
                   a.StatusId, st.StatusName,
                   a.AcademicYear, a.Gwa, a.FamilyIncome, a.DateApplied,
                   a.EncodedBy, u.FullName AS EncodedByName
            FROM Applications a
            INNER JOIN Applicants ap ON ap.ApplicantId = a.ApplicantId
            INNER JOIN Courses c ON c.CourseId = ap.CourseId
            INNER JOIN Scholarships s ON s.ScholarshipId = a.ScholarshipId
            INNER JOIN ApplicationStatus st ON st.StatusId = a.StatusId
            INNER JOIN Users u ON u.UserId = a.EncodedBy";

        public int AddApplication(ScholarshipApplication application)
        {
            string insertApplication = @"INSERT INTO Applications
                            (ApplicantId, ScholarshipId, StatusId, AcademicYear, Gwa,
                             FamilyIncome, DateApplied, EncodedBy)
                            OUTPUT INSERTED.ApplicationId
                            VALUES
                            (@ApplicantId, @ScholarshipId, @StatusId, @AcademicYear, @Gwa,
                             @FamilyIncome, @DateApplied, @EncodedBy)";

            string insertChecklist = @"INSERT INTO ApplicationRequirements (ApplicationId, RequirementId)
                            SELECT @ApplicationId, RequirementId
                            FROM ScholarshipRequirements
                            WHERE ScholarshipId = @ScholarshipId";

            DateTime dateApplied = application.DateApplied == default(DateTime)
                ? DateTime.Now
                : application.DateApplied;

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        int applicationId;

                        using (var cmd = new SqlCommand(insertApplication, con, tx))
                        {
                            cmd.AddParameter("@ApplicantId", SqlDbType.Int, application.ApplicantId);
                            cmd.AddParameter("@ScholarshipId", SqlDbType.Int, application.ScholarshipId);
                            cmd.AddParameter("@StatusId", SqlDbType.Int, (int)ApplicationStatus.Submitted);
                            cmd.AddText("@AcademicYear", 9, application.AcademicYear);
                            cmd.AddDecimal("@Gwa", application.Gwa, 3, 2);
                            cmd.AddDecimal("@FamilyIncome", application.FamilyIncome, 12, 2);
                            cmd.AddParameter("@DateApplied", SqlDbType.DateTime, dateApplied);
                            cmd.AddParameter("@EncodedBy", SqlDbType.Int, application.EncodedBy);

                            applicationId = (int)cmd.ExecuteScalar();
                        }

                        AddHistory(con, tx, applicationId, ApplicationStatus.Submitted,
                                   application.EncodedBy, "Application encoded");

                        using (var cmd = new SqlCommand(insertChecklist, con, tx))
                        {
                            cmd.AddParameter("@ApplicationId", SqlDbType.Int, applicationId);
                            cmd.AddParameter("@ScholarshipId", SqlDbType.Int, application.ScholarshipId);

                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return applicationId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        // GWA and Income lang pwedeng ibahin
        // Kapag lahat kasi binago, new application na siya
        public void UpdateApplication(ScholarshipApplication application)
        {
            string query = @"UPDATE Applications SET Gwa = @Gwa, FamilyIncome = @FamilyIncome
                            WHERE ApplicationId = @ApplicationId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddDecimal("@Gwa", application.Gwa, 3, 2);
                    cmd.AddDecimal("@FamilyIncome", application.FamilyIncome, 12, 2);
                    cmd.AddParameter("@ApplicationId", SqlDbType.Int, application.ApplicationId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ChangeStatus(int applicationId, ApplicationStatus newStatus, int changedBy, string remarks)
        {
            string query = @"UPDATE Applications SET StatusId = @StatusId
                            WHERE ApplicationId = @ApplicationId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(query, con, tx))
                        {
                            cmd.AddParameter("@StatusId", SqlDbType.Int, (int)newStatus);
                            cmd.AddParameter("@ApplicationId", SqlDbType.Int, applicationId);

                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                throw new InvalidOperationException("Application not found.");
                            }
                        }

                        AddHistory(con, tx, applicationId, newStatus, changedBy, remarks);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<ScholarshipApplication> Search(ApplicationFilter filter)
        {
            filter = filter ?? new ApplicationFilter();

            string query = SelectApplications + @"
                            WHERE (@ScholarshipId IS NULL OR a.ScholarshipId = @ScholarshipId)
                            AND (@StatusId IS NULL OR a.StatusId = @StatusId)
                            AND (@CourseId IS NULL OR ap.CourseId = @CourseId)
                            AND (@DateFrom IS NULL OR a.DateApplied >= @DateFrom)
                            AND (@DateTo IS NULL OR a.DateApplied < DATEADD(DAY, 1, @DateTo))
                            AND (@Like IS NULL OR ap.StudentNo LIKE @Like
                                 OR ap.FirstName LIKE @Like OR ap.LastName LIKE @Like)
                            ORDER BY a.DateApplied DESC, a.ApplicationId DESC";

            string like = string.IsNullOrWhiteSpace(filter.Keyword)
                ? null
                : "%" + filter.Keyword.Trim() + "%";

            List<ScholarshipApplication> applications = new List<ScholarshipApplication>();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ScholarshipId", SqlDbType.Int, filter.ScholarshipId);
                    cmd.AddParameter("@StatusId", SqlDbType.Int, (int?)filter.Status);
                    cmd.AddParameter("@CourseId", SqlDbType.Int, filter.CourseId);
                    cmd.AddParameter("@DateFrom", SqlDbType.Date, filter.DateFrom?.Date);
                    cmd.AddParameter("@DateTo", SqlDbType.Date, filter.DateTo?.Date);
                    cmd.AddText("@Like", 120, like, true);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            applications.Add(MapApplication(reader));
                        }
                    }
                }
            }

            return applications;
        }

        public ScholarshipApplication GetById(int id)
        {
            string query = SelectApplications + " WHERE a.ApplicationId = @ApplicationId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ApplicationId", SqlDbType.Int, id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null; // Application not found
                        }

                        return MapApplication(reader);
                    }
                }
            }
        }

        // pang-check ng mga duplicate applications (same applicant, same scholarship, same academic year)
        public bool Exists(int applicantId, int scholarshipId, string academicYear)
        {
            string query = @"SELECT COUNT(*) FROM Applications
                            WHERE ApplicantId = @ApplicantId
                            AND ScholarshipId = @ScholarshipId
                            AND AcademicYear = @AcademicYear";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ApplicantId", SqlDbType.Int, applicantId);
                    cmd.AddParameter("@ScholarshipId", SqlDbType.Int, scholarshipId);
                    cmd.AddText("@AcademicYear", 9, academicYear);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        // Slots are counted per academic year so last year's grantees don't use up this year's slots.
        public int GetApprovedCount(int scholarshipId, string academicYear)
        {
            string query = @"SELECT COUNT(*) FROM Applications
                            WHERE ScholarshipId = @ScholarshipId
                            AND AcademicYear = @AcademicYear
                            AND StatusId = @StatusId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ScholarshipId", SqlDbType.Int, scholarshipId);
                    cmd.AddText("@AcademicYear", 9, academicYear);
                    cmd.AddParameter("@StatusId", SqlDbType.Int, (int)ApplicationStatus.Approved);

                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        // Newest change first.
        public List<StatusHistoryEntry> GetHistory(int applicationId)
        {
            string query = @"SELECT h.HistoryId, h.ApplicationId, h.StatusId, st.StatusName,
                            h.ChangedBy, u.FullName AS ChangedByName, h.ChangedAt, h.Remarks
                            FROM ApplicationStatusHistory h
                            INNER JOIN ApplicationStatus st ON st.StatusId = h.StatusId
                            INNER JOIN Users u ON u.UserId = h.ChangedBy
                            WHERE h.ApplicationId = @ApplicationId
                            ORDER BY h.ChangedAt DESC, h.HistoryId DESC";

            List<StatusHistoryEntry> history = new List<StatusHistoryEntry>();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ApplicationId", SqlDbType.Int, applicationId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history.Add(new StatusHistoryEntry
                            {
                                HistoryId = Convert.ToInt32(reader["HistoryId"]),
                                ApplicationId = Convert.ToInt32(reader["ApplicationId"]),
                                Status = (ApplicationStatus)Convert.ToInt32(reader["StatusId"]),
                                StatusName = reader["StatusName"].ToString(),
                                ChangedBy = Convert.ToInt32(reader["ChangedBy"]),
                                ChangedByName = reader["ChangedByName"].ToString(),
                                ChangedAt = Convert.ToDateTime(reader["ChangedAt"]),
                                Remarks = reader["Remarks"] as string
                            });
                        }
                    }
                }
            }

            return history;
        }

        public List<ApplicationRequirement> GetChecklist(int applicationId)
        {
            string query = @"SELECT ar.ApplicationId, ar.RequirementId, r.Name AS RequirementName,
                            ar.IsSubmitted, ar.DateSubmitted, ar.VerifiedBy,
                            u.FullName AS VerifiedByName
                            FROM ApplicationRequirements ar
                            INNER JOIN Requirements r ON r.RequirementId = ar.RequirementId
                            LEFT JOIN Users u ON u.UserId = ar.VerifiedBy
                            WHERE ar.ApplicationId = @ApplicationId
                            ORDER BY r.Name";

            List<ApplicationRequirement> checklist = new List<ApplicationRequirement>();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ApplicationId", SqlDbType.Int, applicationId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            checklist.Add(new ApplicationRequirement
                            {
                                ApplicationId = Convert.ToInt32(reader["ApplicationId"]),
                                RequirementId = Convert.ToInt32(reader["RequirementId"]),
                                RequirementName = reader["RequirementName"].ToString(),
                                IsSubmitted = Convert.ToBoolean(reader["IsSubmitted"]),
                                DateSubmitted = reader["DateSubmitted"] as DateTime?,
                                VerifiedBy = reader["VerifiedBy"] as int?,
                                VerifiedByName = reader["VerifiedByName"] as string
                            });
                        }
                    }
                }
            }

            return checklist;
        }

        public void SetRequirementSubmitted(int applicationId, int requirementId, bool isSubmitted, int userId)
        {
            string query = @"UPDATE ApplicationRequirements
                            SET IsSubmitted = @IsSubmitted,
                                DateSubmitted = CASE WHEN @IsSubmitted = 1 THEN CAST(GETDATE() AS DATE) ELSE NULL END,
                                VerifiedBy = CASE WHEN @IsSubmitted = 1 THEN @UserId ELSE NULL END
                            WHERE ApplicationId = @ApplicationId AND RequirementId = @RequirementId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@IsSubmitted", SqlDbType.Bit, isSubmitted);
                    cmd.AddParameter("@UserId", SqlDbType.Int, userId);
                    cmd.AddParameter("@ApplicationId", SqlDbType.Int, applicationId);
                    cmd.AddParameter("@RequirementId", SqlDbType.Int, requirementId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 0 means the checklist is complete (also true if the scholarship requires nothing).
        public int CountMissingRequirements(int applicationId)
        {
            string query = @"SELECT COUNT(*) FROM ApplicationRequirements
                            WHERE ApplicationId = @ApplicationId AND IsSubmitted = 0";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ApplicationId", SqlDbType.Int, applicationId);

                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        // Runs inside the caller's transaction (AddApplication and ChangeStatus).
        private static void AddHistory(SqlConnection con, SqlTransaction tx, int applicationId,
                                       ApplicationStatus status, int changedBy, string remarks)
        {
            string query = @"INSERT INTO ApplicationStatusHistory (ApplicationId, StatusId, ChangedBy, Remarks)
                            VALUES (@ApplicationId, @StatusId, @ChangedBy, @Remarks)";

            using (var cmd = new SqlCommand(query, con, tx))
            {
                cmd.AddParameter("@ApplicationId", SqlDbType.Int, applicationId);
                cmd.AddParameter("@StatusId", SqlDbType.Int, (int)status);
                cmd.AddParameter("@ChangedBy", SqlDbType.Int, changedBy);
                cmd.AddText("@Remarks", 500, remarks, true);

                cmd.ExecuteNonQuery();
            }
        }

        private static ScholarshipApplication MapApplication(SqlDataReader reader)
        {
            return new ScholarshipApplication
            {
                ApplicationId = Convert.ToInt32(reader["ApplicationId"]),
                ApplicantId = Convert.ToInt32(reader["ApplicantId"]),
                ApplicantName = reader["ApplicantName"].ToString(),
                StudentNo = reader["StudentNo"].ToString(),
                CourseCode = reader["CourseCode"].ToString(),
                ScholarshipId = Convert.ToInt32(reader["ScholarshipId"]),
                ScholarshipName = reader["ScholarshipName"].ToString(),
                Status = (ApplicationStatus)Convert.ToInt32(reader["StatusId"]),
                StatusName = reader["StatusName"].ToString(),
                AcademicYear = reader["AcademicYear"].ToString(),
                Gwa = Convert.ToDecimal(reader["Gwa"]),
                FamilyIncome = Convert.ToDecimal(reader["FamilyIncome"]),
                DateApplied = Convert.ToDateTime(reader["DateApplied"]),
                EncodedBy = Convert.ToInt32(reader["EncodedBy"]),
                EncodedByName = reader["EncodedByName"].ToString()
            };
        }
    }
}
