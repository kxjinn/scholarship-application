using Microsoft.Data.SqlClient;
using scholarship_Application.Models;
using scholarship_Application.Repositories.Interfaces;
using scholarship_Application.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace scholarship_Application.Repositories
{
    public class ScholarshipRepository : IScholarshipRepository
    {
        private const string SelectColumns = @"
            SELECT ScholarshipId, Name, Description, Sponsor, AmountPerSem, Slots,
                   MinGwa, MaxFamilyIncome, OpenDate, CloseDate, IsActive
            FROM Scholarships";

        public int AddScholarship(Scholarship scholarship)
        {
            string query = @"INSERT INTO Scholarships
                            (Name, Description, Sponsor, AmountPerSem, Slots, MinGwa,
                             MaxFamilyIncome, OpenDate, CloseDate, IsActive)
                            OUTPUT INSERTED.ScholarshipId
                            VALUES
                            (@Name, @Description, @Sponsor, @AmountPerSem, @Slots, @MinGwa,
                             @MaxFamilyIncome, @OpenDate, @CloseDate, 1)";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        int scholarshipId;

                        using (var cmd = new SqlCommand(query, con, tx))
                        {
                            AddScholarshipParameters(cmd, scholarship);
                            scholarshipId = (int)cmd.ExecuteScalar();
                        }

                        SaveRequirementLinks(con, tx, scholarshipId, scholarship.Requirements);

                        tx.Commit();
                        return scholarshipId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public void UpdateScholarship(Scholarship scholarship)
        {
            string query = @"UPDATE Scholarships SET Name = @Name, Description = @Description,
                            Sponsor = @Sponsor, AmountPerSem = @AmountPerSem, Slots = @Slots,
                            MinGwa = @MinGwa, MaxFamilyIncome = @MaxFamilyIncome,
                            OpenDate = @OpenDate, CloseDate = @CloseDate
                            WHERE ScholarshipId = @ScholarshipId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(query, con, tx))
                        {
                            AddScholarshipParameters(cmd, scholarship);
                            cmd.AddParameter("@ScholarshipId", SqlDbType.Int, scholarship.ScholarshipId);
                            cmd.ExecuteNonQuery();
                        }

                        SaveRequirementLinks(con, tx, scholarship.ScholarshipId, scholarship.Requirements);

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

        public void SetScholarshipActive(int scholarshipId, bool isActive)
        {
            string query = @"UPDATE Scholarships SET IsActive = @IsActive
                            WHERE ScholarshipId = @ScholarshipId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@IsActive", SqlDbType.Bit, isActive);
                    cmd.AddParameter("@ScholarshipId", SqlDbType.Int, scholarshipId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Scholarship> GetAll(bool activeOnly)
        {
            string query = SelectColumns + @"
                            WHERE (@ActiveOnly = 0 OR IsActive = 1)
                            ORDER BY OpenDate DESC, Name";

            List<Scholarship> scholarships = new List<Scholarship>();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ActiveOnly", SqlDbType.Bit, activeOnly);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            scholarships.Add(MapScholarship(reader));
                        }
                    }
                }
            }

            return scholarships;
        }

        public List<Scholarship> GetOpenOn(DateTime date)
        {
            string query = SelectColumns + @"
                            WHERE IsActive = 1 AND @Date >= OpenDate AND @Date <= CloseDate
                            ORDER BY Name";

            List<Scholarship> scholarships = new List<Scholarship>();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@Date", SqlDbType.Date, date.Date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            scholarships.Add(MapScholarship(reader));
                        }
                    }
                }
            }

            return scholarships;
        }

        public Scholarship GetById(int id)
        {
            string query = SelectColumns + " WHERE ScholarshipId = @ScholarshipId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                Scholarship scholarship = null;

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@ScholarshipId", SqlDbType.Int, id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            scholarship = MapScholarship(reader);
                        }
                    }
                }

                if (scholarship == null)
                {
                    return null; // Scholarship not found
                }

                // Reader is closed by now, so the same connection can be reused.
                scholarship.Requirements = ReadRequirements(con, id);
                return scholarship;
            }
        }

        public List<Requirement> GetRequirements(int scholarshipId)
        {
            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                return ReadRequirements(con, scholarshipId);
            }
        }

        public bool NameExists(string name, int excludeScholarshipId = 0)
        {
            string query = @"SELECT COUNT(*) FROM Scholarships
                            WHERE Name = @Name AND ScholarshipId <> @ExcludeScholarshipId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddText("@Name", 100, name);
                    cmd.AddParameter("@ExcludeScholarshipId", SqlDbType.Int, excludeScholarshipId);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        private static void AddScholarshipParameters(SqlCommand cmd, Scholarship scholarship)
        {
            cmd.AddText("@Name", 100, scholarship.Name);
            cmd.AddText("@Description", 500, scholarship.Description, true);
            cmd.AddText("@Sponsor", 100, scholarship.Sponsor, true);
            cmd.AddDecimal("@AmountPerSem", scholarship.AmountPerSem, 10, 2);
            cmd.AddParameter("@Slots", SqlDbType.Int, scholarship.Slots);
            cmd.AddDecimal("@MinGwa", scholarship.MinGwa, 3, 2);
            cmd.AddDecimal("@MaxFamilyIncome", scholarship.MaxFamilyIncome, 12, 2);
            cmd.AddParameter("@OpenDate", SqlDbType.Date, scholarship.OpenDate.Date);
            cmd.AddParameter("@CloseDate", SqlDbType.Date, scholarship.CloseDate.Date);
        }

        private static void SaveRequirementLinks(SqlConnection con, SqlTransaction tx,
                                                 int scholarshipId, List<Requirement> requirements)
        {
            using (var delete = new SqlCommand(
                "DELETE FROM ScholarshipRequirements WHERE ScholarshipId = @ScholarshipId", con, tx))
            {
                delete.AddParameter("@ScholarshipId", SqlDbType.Int, scholarshipId);
                delete.ExecuteNonQuery();
            }

            if (requirements == null || requirements.Count == 0)
            {
                return;
            }

            using (var insert = new SqlCommand(
                @"INSERT INTO ScholarshipRequirements (ScholarshipId, RequirementId)
                  VALUES (@ScholarshipId, @RequirementId)", con, tx))
            {
                insert.AddParameter("@ScholarshipId", SqlDbType.Int, scholarshipId);
                SqlParameter requirementId = insert.AddParameter("@RequirementId", SqlDbType.Int, 0);

                // Distinct() para ma-stop ang duplicate requirement IDs na baka naipasa sa list
                foreach (int id in requirements.Select(r => r.RequirementId).Distinct())
                {
                    requirementId.Value = id;
                    insert.ExecuteNonQuery();
                }
            }
        }

        private static List<Requirement> ReadRequirements(SqlConnection con, int scholarshipId)
        {
            string query = @"SELECT r.RequirementId, r.Name
                            FROM ScholarshipRequirements sr
                            INNER JOIN Requirements r ON r.RequirementId = sr.RequirementId
                            WHERE sr.ScholarshipId = @ScholarshipId
                            ORDER BY r.Name";

            List<Requirement> requirements = new List<Requirement>();

            using (var cmd = new SqlCommand(query, con))
            {
                cmd.AddParameter("@ScholarshipId", SqlDbType.Int, scholarshipId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        requirements.Add(new Requirement
                        {
                            RequirementId = Convert.ToInt32(reader["RequirementId"]),
                            Name = reader["Name"].ToString()
                        });
                    }
                }
            }

            return requirements;
        }

        private static Scholarship MapScholarship(SqlDataReader reader)
        {
            return new Scholarship
            {
                ScholarshipId = Convert.ToInt32(reader["ScholarshipId"]),
                Name = reader["Name"].ToString(),
                Description = reader["Description"] as string,
                Sponsor = reader["Sponsor"] as string,
                AmountPerSem = Convert.ToDecimal(reader["AmountPerSem"]),
                Slots = Convert.ToInt32(reader["Slots"]),
                MinGwa = reader["MinGwa"] as decimal?,
                MaxFamilyIncome = reader["MaxFamilyIncome"] as decimal?,
                OpenDate = Convert.ToDateTime(reader["OpenDate"]),
                CloseDate = Convert.ToDateTime(reader["CloseDate"]),
                IsActive = Convert.ToBoolean(reader["IsActive"])
            };
        }
    }
}
