﻿using CarCareTracker.External.Implementations;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using LiteDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data.Common;
using System.Xml.Linq;

namespace CarCareTracker.Controllers
{
    [Authorize(Roles = nameof(UserData.IsRootUser))]
    public class MigrationController : Controller
    {
        private IConfigHelper _configHelper;
        private IConfiguration _serverConfig;
        private IFileHelper _fileHelper;
        private readonly ILogger<MigrationController> _logger;
        public MigrationController(IConfigHelper configHelper, IFileHelper fileHelper, IConfiguration serverConfig, ILogger<MigrationController> logger)
        {
            _configHelper = configHelper;
            _fileHelper = fileHelper;
            _serverConfig = serverConfig;
            _logger = logger;
        }
        public IActionResult Index()
        {
            if (_configHelper.GetServerHasPostgresConnection())
            {
                return View();
            } else
            {
                return new RedirectResult("/Error/Unauthorized");
            }
        }
        private void InitializeTables(NpgsqlConnection conn)
        {
            var cmds = new List<string>
            {
                "CREATE TABLE IF NOT EXISTS app.vehicles (id INT GENERATED BY DEFAULT AS IDENTITY primary key, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.collisionrecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.upgraderecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.servicerecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.gasrecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.notes (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.odometerrecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.reminderrecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.planrecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.planrecordtemplates (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.supplyrecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.taxrecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.userrecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, username TEXT not null, emailaddress TEXT not null, password TEXT not null, isadmin BOOLEAN)",
                "CREATE TABLE IF NOT EXISTS app.tokenrecords (id INT GENERATED BY DEFAULT AS IDENTITY primary key, body TEXT not null, emailaddress TEXT not null)",
                "CREATE TABLE IF NOT EXISTS app.userconfigrecords (id INT primary key, data jsonb not null)",
                "CREATE TABLE IF NOT EXISTS app.useraccessrecords (userId INT, vehicleId INT, PRIMARY KEY(userId, vehicleId))"
            };
            foreach(string cmd in cmds)
            {
                using (var ctext = new NpgsqlCommand(cmd, conn))
                {
                    ctext.ExecuteNonQuery();
                }
            }
        }
        public IActionResult Import(string fileName)
        {
            if (!_configHelper.GetServerHasPostgresConnection())
            {
                return new RedirectResult("/Error/Unauthorized");
            }
            var fullFileName = _fileHelper.GetFullFilePath(fileName);
            if (string.IsNullOrWhiteSpace(fullFileName))
            {
                return Json(new OperationResponse { Success = false, Message = StaticHelper.GenericErrorMessage });
            }
            try
            {
                var pgDataSource = new NpgsqlConnection(_serverConfig["POSTGRES_CONNECTION"]);
                pgDataSource.Open();
                InitializeTables(pgDataSource);
                //pull records
                var vehicles = new List<Vehicle>();
                var repairrecords = new List<CollisionRecord>();
                var upgraderecords = new List<UpgradeRecord>();
                var servicerecords = new List<ServiceRecord>();

                var gasrecords = new List<GasRecord>();
                var noterecords = new List<Note>();
                var odometerrecords = new List<OdometerRecord>();
                var reminderrecords = new List<ReminderRecord>();

                var planrecords = new List<PlanRecord>();
                var planrecordtemplates = new List<PlanRecordInput>();
                var supplyrecords = new List<SupplyRecord>();
                var taxrecords = new List<TaxRecord>();

                var userrecords = new List<UserData>();
                var tokenrecords = new List<Token>();
                var userconfigrecords = new List<UserConfigData>();
                var useraccessrecords = new List<UserAccess>();
                #region "Part1"
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<Vehicle>("vehicles");
                    vehicles = table.FindAll().ToList();
                };
                foreach(var vehicle in vehicles)
                {
                    string cmd = $"INSERT INTO app.vehicles (id, data) VALUES(@id, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", vehicle.Id);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(vehicle));
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<CollisionRecord>("collisionrecords");
                    repairrecords = table.FindAll().ToList();
                };
                foreach (var record in repairrecords)
                {
                    string cmd = $"INSERT INTO app.collisionrecords (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<ServiceRecord>("servicerecords");
                    servicerecords = table.FindAll().ToList();
                };
                foreach (var record in servicerecords)
                {
                    string cmd = $"INSERT INTO app.servicerecords (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<UpgradeRecord>("upgraderecords");
                    upgraderecords = table.FindAll().ToList();
                };
                foreach (var record in upgraderecords)
                {
                    string cmd = $"INSERT INTO app.upgraderecords (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                #endregion
                #region "Part2"
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<GasRecord>("gasrecords");
                    gasrecords = table.FindAll().ToList();
                };
                foreach (var record in gasrecords)
                {
                    string cmd = $"INSERT INTO app.gasrecords (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<Note>("notes");
                    noterecords = table.FindAll().ToList();
                };
                foreach (var record in noterecords)
                {
                    string cmd = $"INSERT INTO app.notes (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<OdometerRecord>("odometerrecords");
                    odometerrecords = table.FindAll().ToList();
                };
                foreach (var record in odometerrecords)
                {
                    string cmd = $"INSERT INTO app.odometerrecords (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<ReminderRecord>("reminderrecords");
                    reminderrecords = table.FindAll().ToList();
                };
                foreach (var record in reminderrecords)
                {
                    string cmd = $"INSERT INTO app.reminderrecords (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                #endregion
                #region "Part3"
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<PlanRecord>("planrecords");
                    planrecords = table.FindAll().ToList();
                };
                foreach (var record in planrecords)
                {
                    string cmd = $"INSERT INTO app.planrecords (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<PlanRecordInput>("planrecordtemplates");
                    planrecordtemplates = table.FindAll().ToList();
                };
                foreach (var record in planrecordtemplates)
                {
                    string cmd = $"INSERT INTO app.planrecordtemplates (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<SupplyRecord>("supplyrecords");
                    supplyrecords = table.FindAll().ToList();
                };
                foreach (var record in supplyrecords)
                {
                    string cmd = $"INSERT INTO app.supplyrecords (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<TaxRecord>("taxrecords");
                    taxrecords = table.FindAll().ToList();
                };
                foreach (var record in taxrecords)
                {
                    string cmd = $"INSERT INTO app.taxrecords (id, vehicleId, data) VALUES(@id, @vehicleId, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("vehicleId", record.VehicleId);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                #endregion
                #region "Part4"
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<UserData>("userrecords");
                    userrecords =  table.FindAll().ToList();
                };
                foreach (var record in userrecords)
                {
                    string cmd = $"INSERT INTO app.userrecords (id, username, emailaddress, password, isadmin) VALUES(@id, @username, @emailaddress, @password, @isadmin)";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("username", record.UserName);
                        ctext.Parameters.AddWithValue("emailaddress", record.EmailAddress);
                        ctext.Parameters.AddWithValue("password", record.Password);
                        ctext.Parameters.AddWithValue("isadmin", record.IsAdmin);
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<Token>("tokenrecords");
                    tokenrecords = table.FindAll().ToList();
                };
                foreach (var record in tokenrecords)
                {
                    string cmd = $"INSERT INTO app.tokenrecords (id, emailaddress, body) VALUES(@id, @emailaddress, @body)";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("emailaddress", record.EmailAddress);
                        ctext.Parameters.AddWithValue("body", record.Body);
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<UserConfigData>("userconfigrecords");
                    userconfigrecords = table.FindAll().ToList();
                };
                foreach (var record in userconfigrecords)
                {
                    string cmd = $"INSERT INTO app.userconfigrecords (id, data) VALUES(@id, CAST(@data AS jsonb))";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("id", record.Id);
                        ctext.Parameters.AddWithValue("data", System.Text.Json.JsonSerializer.Serialize(record));
                        ctext.ExecuteNonQuery();
                    }
                }
                using (var db = new LiteDatabase(fullFileName))
                {
                    var table = db.GetCollection<UserAccess>("useraccessrecords");
                    useraccessrecords = table.FindAll().ToList();
                };
                foreach (var record in useraccessrecords)
                {
                    string cmd = $"INSERT INTO app.useraccessrecords (userId, vehicleId) VALUES(@userId, @vehicleId)";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("userId", record.Id.UserId);
                        ctext.Parameters.AddWithValue("vehicleId", record.Id.VehicleId);
                        ctext.ExecuteNonQuery();
                    }
                }
                #endregion
                return Json(new OperationResponse { Success = true, Message = "Data Imported Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(new OperationResponse { Success = false, Message = StaticHelper.GenericErrorMessage });
            }
        }
    }
}
