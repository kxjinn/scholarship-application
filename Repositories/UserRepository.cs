using Microsoft.Data.SqlClient;
using scholarship_Application.Models;
using scholarship_Application.Repositories.Interfaces;
using scholarship_Application.Utilities;
using System;
using System.Collections.Generic;
using System.Data;

namespace scholarship_Application.Repositories
{
    public class UserRepository : IUserRepository
    {
        // wala 'yung Password column so list screens (mga grid) can never leak it.
        private const string SelectUsers = @"
            SELECT u.UserId, u.RoleId, r.RoleName, u.Username, u.FullName, u.IsActive, u.CreatedAt
            FROM Users u
            INNER JOIN Roles r ON r.RoleId = u.RoleId";

        public int AddUser(User user)
        {
            string query = @"INSERT INTO Users (RoleId, Username, [Password], FullName, IsActive)
                            OUTPUT INSERTED.UserId
                            VALUES (@RoleId, @Username, @Password, @FullName, 1)";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@RoleId", SqlDbType.Int, user.RoleId);
                    cmd.AddText("@Username", 50, user.Username);
                    cmd.AddParameter("@Password", SqlDbType.NVarChar, user.Password, 255);
                    cmd.AddText("@FullName", 100, user.FullName);

                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public void UpdateUser(User user)
        {
            string query = @"UPDATE Users SET RoleId = @RoleId, FullName = @FullName
                            WHERE UserId = @UserId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@RoleId", SqlDbType.Int, user.RoleId);
                    cmd.AddText("@FullName", 100, user.FullName);
                    cmd.AddParameter("@UserId", SqlDbType.Int, user.UserId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Soft delete: history (EncodedBy, ChangedBy, VerifiedBy) keeps pointing at this user.
        public void SetUserActive(int userId, bool isActive)
        {
            string query = @"UPDATE Users SET IsActive = @IsActive WHERE UserId = @UserId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@IsActive", SqlDbType.Bit, isActive);
                    cmd.AddParameter("@UserId", SqlDbType.Int, userId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ResetPassword(int userId, string newPassword)
        {
            string query = @"UPDATE Users SET [Password] = @Password WHERE UserId = @UserId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@Password", SqlDbType.NVarChar, newPassword, 255);
                    cmd.AddParameter("@UserId", SqlDbType.Int, userId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<User> GetAll()
        {
            string query = SelectUsers + " ORDER BY u.FullName";

            List<User> users = new List<User>();

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(MapUser(reader));
                        }
                    }
                }
            }

            return users;
        }

        public User GetById(int id)
        {
            string query = SelectUsers + " WHERE u.UserId = @UserId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddParameter("@UserId", SqlDbType.Int, id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null; // User not found
                        }

                        return MapUser(reader);
                    }
                }
            }
        }

        public User GetActiveByUsername(string username)
        {
            string query = @"SELECT u.UserId, u.RoleId, r.RoleName, u.Username, u.FullName,
                            u.IsActive, u.CreatedAt, u.[Password]
                            FROM Users u
                            INNER JOIN Roles r ON r.RoleId = u.RoleId
                            WHERE u.Username = @Username AND u.IsActive = 1";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddText("@Username", 50, username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null; // No such active user
                        }

                        User user = MapUser(reader);
                        user.Password = reader["Password"].ToString();
                        return user;
                    }
                }
            }
        }

        public bool UsernameExists(string username, int excludeUserId = 0)
        {
            string query = @"SELECT COUNT(*) FROM Users
                            WHERE Username = @Username AND UserId <> @ExcludeUserId";

            using (var con = DbConnection.GetConnection())
            {
                con.Open();

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.AddText("@Username", 50, username);
                    cmd.AddParameter("@ExcludeUserId", SqlDbType.Int, excludeUserId);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                UserId = Convert.ToInt32(reader["UserId"]),
                RoleId = Convert.ToInt32(reader["RoleId"]),
                RoleName = reader["RoleName"].ToString(),
                Username = reader["Username"].ToString(),
                FullName = reader["FullName"].ToString(),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }
    }
}
