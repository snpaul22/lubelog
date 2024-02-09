﻿using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;
using Npgsql;

namespace CarCareTracker.External.Implementations
{
    public class PGUserRecordDataAccess : IUserRecordDataAccess
    {
        private NpgsqlConnection pgDataSource;
        private readonly ILogger<PGUserRecordDataAccess> _logger;
        private static string tableName = "userrecords";
        public PGUserRecordDataAccess(IConfiguration config, ILogger<PGUserRecordDataAccess> logger)
        {
            pgDataSource = new NpgsqlConnection(config["POSTGRES_CONNECTION"]);
            _logger = logger;
            try
            {
                pgDataSource.Open();
                //create table if not exist.
                string initCMD = $"CREATE TABLE IF NOT EXISTS app.{tableName} (id INT GENERATED ALWAYS AS IDENTITY primary key, username TEXT not null, emailaddress TEXT not null, password TEXT not null, isadmin BOOLEAN)";
                using (var ctext = new NpgsqlCommand(initCMD, pgDataSource))
                {
                    ctext.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        public List<UserData> GetUsers()
        {
            try
            {
                string cmd = $"SELECT id, username, emailaddress, password, isadmin FROM app.{tableName}";
                var results = new List<UserData>();
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            UserData result = new UserData();
                            result.Id = int.Parse(reader["id"].ToString());
                            result.UserName = reader["username"].ToString();
                            result.EmailAddress = reader["emailaddress"].ToString();
                            result.Password = reader["password"].ToString();
                            result.IsAdmin = bool.Parse(reader["isadmin"].ToString());
                            results.Add(result);
                        }
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<UserData>();
            }
        }
        public UserData GetUserRecordByUserName(string userName)
        {
            try
            {
                string cmd = $"SELECT id, username, emailaddress, password, isadmin FROM app.{tableName} WHERE username = @username";
                var result = new UserData();
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("username", userName);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            result.Id = int.Parse(reader["id"].ToString());
                            result.UserName = reader["username"].ToString();
                            result.EmailAddress = reader["emailaddress"].ToString();
                            result.Password = reader["password"].ToString();
                            result.IsAdmin = bool.Parse(reader["isadmin"].ToString());
                        }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new UserData();
            }
        }
        public UserData GetUserRecordByEmailAddress(string emailAddress)
        {
            try
            {
                string cmd = $"SELECT id, username, emailaddress, password, isadmin FROM app.{tableName} WHERE emailaddress = @emailaddress";
                var result = new UserData();
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("emailaddress", emailAddress);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            result.Id = int.Parse(reader["id"].ToString());
                            result.UserName = reader["username"].ToString();
                            result.EmailAddress = reader["emailaddress"].ToString();
                            result.Password = reader["password"].ToString();
                            result.IsAdmin = bool.Parse(reader["isadmin"].ToString());
                        }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new UserData();
            }
        }
        public UserData GetUserRecordById(int userId)
        {
            try
            {
                string cmd = $"SELECT id, username, emailaddress, password, isadmin FROM app.{tableName} WHERE id = @id";
                var result = new UserData();
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("id", userId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            result.Id = int.Parse(reader["id"].ToString());
                            result.UserName = reader["username"].ToString();
                            result.EmailAddress = reader["emailaddress"].ToString();
                            result.Password = reader["password"].ToString();
                            result.IsAdmin = bool.Parse(reader["isadmin"].ToString());
                        }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new UserData();
            }
        }
        public bool SaveUserRecord(UserData userRecord)
        {
            try
            {
                if (userRecord.Id == default)
                {
                    string cmd = $"INSERT INTO app.{tableName} (username, emailaddress, password, isadmin) VALUES(@username, @emailaddress, @password, @isadmin) RETURNING id";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("username", userRecord.UserName);
                        ctext.Parameters.AddWithValue("emailaddress", userRecord.EmailAddress);
                        ctext.Parameters.AddWithValue("password", userRecord.Password);
                        ctext.Parameters.AddWithValue("isadmin", userRecord.IsAdmin);
                        userRecord.Id = Convert.ToInt32(ctext.ExecuteScalar());
                        return userRecord.Id != default;
                    }
                }
                else
                {
                    string cmd = $"UPDATE app.{tableName} SET username = @username, emailaddress = @emailaddress, password = @password, isadmin = @isadmin WHERE id = @id";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", userRecord.Id);
                        ctext.Parameters.AddWithValue("username", userRecord.UserName);
                        ctext.Parameters.AddWithValue("emailaddress", userRecord.EmailAddress);
                        ctext.Parameters.AddWithValue("password", userRecord.Password);
                        ctext.Parameters.AddWithValue("isadmin", userRecord.IsAdmin);
                        return ctext.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
        public bool DeleteUserRecord(int userId)
        {
            try
            {
                string cmd = $"DELETE FROM app.{tableName} WHERE id = @id";
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("id", userId);
                    return ctext.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
    }
}