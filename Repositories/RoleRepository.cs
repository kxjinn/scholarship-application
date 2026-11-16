using Microsoft.Data.SqlClient;
using scholarship_Application.Models;
using scholarship_Application.Repositories.Interfaces;
using scholarship_Application.Utilities;
using System;
using System.Collections.Generic;
using System.Data;

namespace scholarship_Application.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private const string SelectColumns = @"SELECT RoleId, RoleName FROM Roles";

        public List<Role> GetAll()
        {
            string query = SelectColumns + " ORDER BY RoleId";

            List<Role> roles = new List<Role>();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            roles.Add(MapRole(reader));
                        }
                    }
                }
            }

            return roles;
        }

        public Role GetById(int id)
        {
            string query = SelectColumns + " WHERE RoleId = @RoleId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@RoleId", SqlDbType.Int, id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null; // Role not found
                        }

                        return MapRole(reader);
                    }
                }
            }
        }

        private static Role MapRole(SqlDataReader reader)
        {
            return new Role
            {
                RoleId = Convert.ToInt32(reader["RoleId"]),
                RoleName = reader["RoleName"].ToString()
            };
        }
    }
}
