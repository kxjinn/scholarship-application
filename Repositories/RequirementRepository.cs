using Microsoft.Data.SqlClient;
using scholarship_Application.Models;
using scholarship_Application.Repositories.Interfaces;
using scholarship_Application.Utilities;
using System;
using System.Collections.Generic;
using System.Data;

namespace scholarship_Application.Repositories
{
    public class RequirementRepository : IRequirementRepository
    {
        private const string SelectColumns =
            @"SELECT RequirementId, Name FROM Requirements";

        public int AddRequirement(Requirement requirement)
        {
            string query = @"INSERT INTO Requirements (Name)
                            OUTPUT INSERTED.RequirementId
                            VALUES (@Name)";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddText("@Name", 100, requirement.Name);

                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public void UpdateRequirement(Requirement requirement)
        {
            string query = @"UPDATE Requirements SET Name = @Name WHERE RequirementId = @RequirementId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddText("@Name", 100, requirement.Name);
                    cmd.AddParameter("@RequirementId", SqlDbType.Int, requirement.RequirementId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteRequirement(int requirementId)
        {
            string query = @"DELETE FROM Requirements WHERE RequirementId = @RequirementId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@RequirementId", SqlDbType.Int, requirementId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Requirement> GetAll()
        {
            string query = SelectColumns + " ORDER BY Name";

            List<Requirement> requirements = new List<Requirement>();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            requirements.Add(MapRequirement(reader));
                        }
                    }
                }
            }

            return requirements;
        }

        public Requirement GetById(int id)
        {
            string query = SelectColumns + " WHERE RequirementId = @RequirementId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@RequirementId", SqlDbType.Int, id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null; // Requirement not found
                        }

                        return MapRequirement(reader);
                    }
                }
            }
        }

        public bool NameExists(string name, int excludeRequirementId = 0)
        {
            string query = @"SELECT COUNT(*) FROM Requirements
                            WHERE Name = @Name AND RequirementId <> @ExcludeRequirementId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddText("@Name", 100, name);
                    cmd.AddParameter("@ExcludeRequirementId", SqlDbType.Int, excludeRequirementId);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public bool IsRequirementInUse(int requirementId)
        {
            string query = @"SELECT
                            (SELECT COUNT(*) FROM ScholarshipRequirements WHERE RequirementId = @RequirementId)
                          + (SELECT COUNT(*) FROM ApplicationRequirements WHERE RequirementId = @RequirementId)";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@RequirementId", SqlDbType.Int, requirementId);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        private static Requirement MapRequirement(SqlDataReader reader)
        {
            return new Requirement
            {
                RequirementId = Convert.ToInt32(reader["RequirementId"]),
                Name = reader["Name"].ToString()
            };
        }
    }
}
