using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using RequestBotLinux.Models;


namespace RequestBotLinux
{
    public class DataBase
    {
        public event Action MessageAdded;
        private readonly string _connectionString;
        public string DbPath { get; }
        public DataBase(string dbPath)
        {
            try
            {
                DbPath = dbPath;
                var directory = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _connectionString = $"Data Source={dbPath};Version=3;";
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                new Window { Content = new TextBlock { Text = $"Ошибка инициализации БД: {ex.Message}" } }.Show();
                throw;
            }
        }
        private void InitializeDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(@"
        CREATE TABLE IF NOT EXISTS Users (
            Username TEXT PRIMARY KEY,
            FirstName TEXT,
            LastName TEXT
        );

        CREATE TABLE IF NOT EXISTS Messages (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL,
            ChatId INTEGER NOT NULL,
            MessageText TEXT NOT NULL,
            Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
            IsTask BOOLEAN DEFAULT 0,
            IsFromAdmin BOOLEAN DEFAULT 0,
            Status TEXT DEFAULT 'None',
            LastName TEXT,
            CabinetNumber TEXT
        );

        CREATE TABLE IF NOT EXISTS Cabinets (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Number TEXT NOT NULL UNIQUE,
            Description TEXT
        );

        CREATE TABLE IF NOT EXISTS Equipment (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Type TEXT NOT NULL,
            Model TEXT NOT NULL,
            OS TEXT,
            CabinetId INTEGER,
            ResponsibleEmployeeId INTEGER,
            FOREIGN KEY(CabinetId) REFERENCES Cabinets(Id),
            FOREIGN KEY(ResponsibleEmployeeId) REFERENCES Employees(Id)
        );

        CREATE TABLE IF NOT EXISTS Employees (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FirstName TEXT NOT NULL,
            LastName TEXT NOT NULL,
            Position TEXT,
            CabinetId INTEGER,
            Username TEXT,
            FOREIGN KEY(CabinetId) REFERENCES Cabinets(Id),
            FOREIGN KEY(Username) REFERENCES Users(Username)
        );", connection);
                    command.ExecuteNonQuery();
                    var migrateCmd = new SQLiteCommand(
                        @"UPDATE Messages 
                SET IsFromAdmin = 0 
                WHERE IsFromAdmin IS NULL",
                    connection);
                    migrateCmd.ExecuteNonQuery();
                    AddColumnIfNotExists(connection, "Messages", "FirstName", "TEXT");
                    AddColumnIfNotExists(connection, "Messages", "Deadline", "DATETIME DEFAULT (datetime('now','+1 month'))");
                }
            }
            catch (Exception ex)
            {
                new Window { Content = new TextBlock { Text = "Ошибка инициализации базы данных: " + ex.Message } }.Show();

            }
        }
        public async Task AddTaskMessageAsync(User user, long chatId, string lastName, string cabinet, string description, DateTime deadline)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SQLiteCommand(
                    @"INSERT INTO Messages 
                (Username, FirstName, ChatId, MessageText, IsTask, Status, LastName, CabinetNumber, Deadline, Timestamp) 
            VALUES 
                (@username, @firstName, @chatId, @messageText, 1, 'В работе', @lastName, @cabinet, @deadline, @timestamp)",
                    connection);

                command.Parameters.AddWithValue("@username", user.Username ?? "");
                command.Parameters.AddWithValue("@firstName", user.FirstName ?? "N/A"); // Добавлено
                command.Parameters.AddWithValue("@chatId", chatId);
                command.Parameters.AddWithValue("@messageText", description);
                command.Parameters.AddWithValue("@lastName", lastName);
                command.Parameters.AddWithValue("@cabinet", cabinet);
                command.Parameters.AddWithValue("@deadline", deadline.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                await command.ExecuteNonQueryAsync();
            }
            MessageAdded?.Invoke();
        }
        public List<TaskData> GetAllTasks()
        {
            var tasks = new List<TaskData>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    @"SELECT 
                M.Id,
                M.Username,
                M.FirstName,
                M.MessageText,
                M.Status, 
                M.LastName,
                M.CabinetNumber,
                M.Timestamp,
                M.Deadline
            FROM Messages M
            WHERE M.IsTask = 1", connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add(new TaskData
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Username = reader["Username"].ToString(),
                            FirstName = reader["FirstName"].ToString(), // Берем из Messages
                            LastName = reader["LastName"].ToString(),
                            CabinetNumber = reader["CabinetNumber"].ToString(),
                            MessageText = reader["MessageText"].ToString(),
                            Status = reader["Status"].ToString(),
                            Timestamp = DateTime.Parse(reader["Timestamp"].ToString()).ToLocalTime(),
                            Deadline = reader["Deadline"] is DBNull
                                ? DateTime.Parse(reader["Timestamp"].ToString()).AddMonths(1)
                                : DateTime.Parse(reader["Deadline"].ToString())
                        });
                    }
                }
            }
            return tasks;
        }
        public void UpdateTaskStatus(int taskId, string status)
        {

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    "UPDATE Messages SET Status = @status WHERE Id = @id",
                    connection);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@id", taskId);
                command.ExecuteNonQuery();
            }
        }
        public void DeleteTask(int taskId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    "DELETE FROM Messages WHERE Id = @id",
                    connection);
                command.Parameters.AddWithValue("@id", taskId);
                command.ExecuteNonQuery();
            }
        }
        // Временно измените метод добавления сообщений для теста
        public async Task AddMessageAsync(string username, long chatId, string messageText)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SQLiteCommand(
                    @"INSERT INTO Messages 
                (Username, ChatId, MessageText, Timestamp) 
                VALUES 
                (@username, @chatId, @messageText, @timestamp)",
                    connection);

                // Добавьте эти строки:
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@chatId", chatId);
                command.Parameters.AddWithValue("@messageText", messageText);
                command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                await command.ExecuteNonQueryAsync();
            }
        }
        public List<string> GetUniqueUsers()
        {
            var users = new List<string>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT DISTINCT Username FROM Messages WHERE IsFromAdmin = 0", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string username = reader["Username"].ToString();
                        if (!string.IsNullOrEmpty(username))
                            users.Add(username);
                    }
                }
            }
            return users;
        }

        public List<MessageData> GetMessagesByUsername(string username)
        {
            var messages = new List<MessageData>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    @"SELECT 
                MessageText, 
                strftime('%Y-%m-%d %H:%M:%S', Timestamp) as Timestamp,
                IsFromAdmin,
                Username
                FROM Messages 
                WHERE Username = @username
                ORDER BY Timestamp",
                    connection);

                command.Parameters.AddWithValue("@username", username);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        messages.Add(new MessageData
                        {
                            Username = reader["Username"].ToString(),
                            MessageText = reader["MessageText"].ToString(),
                            Timestamp = DateTime.Parse(reader["Timestamp"].ToString()),
                            IsFromAdmin = Convert.ToBoolean(reader["IsFromAdmin"])
                        });
                    }
                }
            }
            return messages;
        }
        public bool CheckCabinetExists(string cabinetNumber)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM Cabinets WHERE Number = @number",
                    connection);
                cmd.Parameters.AddWithValue("@number", cabinetNumber);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
        public long GetChatIdByUsername(string username)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    "SELECT ChatId FROM Messages WHERE Username = @username ORDER BY Timestamp DESC LIMIT 1",
                    connection);
                command.Parameters.AddWithValue("@username", username);
                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt64(result) : 0;
            }
        }
        public async Task AddOutgoingMessageAsync(string username, long chatId, string messageText)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SQLiteCommand(
                    @"INSERT INTO Messages 
        (Username, ChatId, MessageText, IsFromAdmin) 
      VALUES 
        (@username, @chatId, @messageText, 1)", connection);

                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@chatId", chatId);
                command.Parameters.AddWithValue("@messageText", messageText);

                await command.ExecuteNonQueryAsync();
            }
            MessageAdded?.Invoke();
        }
        private void AddColumnIfNotExists(SQLiteConnection connection, string table, string column, string type)
        {
            var cmdCheck = new SQLiteCommand(
                $"PRAGMA table_info({table})",
                connection);

            using (var reader = cmdCheck.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader["name"].ToString() == column) return;
                }
            }

            var cmdAdd = new SQLiteCommand(
                $"ALTER TABLE {table} ADD COLUMN {column} {type}",
                connection);
            cmdAdd.ExecuteNonQuery();
        }
        public int DeleteMessagesByPeriod(string username, TimeSpan period)
        {
            try
            {
                var cutoff = DateTime.Now - period;

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
            DELETE FROM Messages 
            WHERE Username = @Username 
            AND Timestamp < @Cutoff 
            AND NOT (IsTask = 1 AND Status != 'Completed')", conn);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Cutoff", cutoff.ToString("yyyy-MM-dd HH:mm:ss"));
                    return cmd.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DELETE ERROR] {ex}");
                return 0;
            }
        }
        public List<MessageData> GetAllMessages()
        {
            var messages = new List<MessageData>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    @"SELECT Username, MessageText, Timestamp, IsFromAdmin 
              FROM Messages 
              ORDER BY Timestamp DESC",
                    connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        messages.Add(new MessageData
                        {
                            Username = reader["Username"].ToString(),
                            MessageText = reader["MessageText"].ToString(),
                            Timestamp = DateTime.Parse(reader["Timestamp"].ToString()),
                            IsFromAdmin = Convert.ToBoolean(reader["IsFromAdmin"])
                        });
                    }
                }
            }
            return messages;
        }
        // Получение всех кабинетов
        public List<Cabinet> GetAllCabinets()
        {
            var cabinets = new List<Cabinet>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand("SELECT * FROM Cabinets", connection);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cabinets.Add(new Cabinet
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Number = reader["Number"].ToString(),
                            Description = reader["Description"].ToString()
                        });
                    }
                }
            }
            return cabinets;
        }

        // Добавление кабинета
        public void AddCabinet(Cabinet cabinet)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand(
                    "INSERT INTO Cabinets (Number, Description) VALUES (@num, @desc)",
                    connection);
                cmd.Parameters.AddWithValue("@num", cabinet.Number);
                cmd.Parameters.AddWithValue("@desc", cabinet.Description);
                cmd.ExecuteNonQuery();
            }
        }
        public void UpdateCabinet(Cabinet cabinet)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand(
                    "UPDATE Cabinets SET Number = @num, Description = @desc WHERE Id = @id",
                    connection);
                cmd.Parameters.AddWithValue("@num", cabinet.Number);
                cmd.Parameters.AddWithValue("@desc", cabinet.Description);
                cmd.Parameters.AddWithValue("@id", cabinet.Id);
                cmd.ExecuteNonQuery();
            }
        }
        public void AddEmployee(Employees employee)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand(
                    "INSERT INTO Employees (FirstName, LastName, Position, CabinetId, Username) " +
                    "VALUES (@fn, @ln, @pos, @cid, @user)", connection);

                cmd.Parameters.AddWithValue("@fn", employee.FirstName);
                cmd.Parameters.AddWithValue("@ln", employee.LastName);
                cmd.Parameters.AddWithValue("@pos", employee.Position);
                cmd.Parameters.AddWithValue("@cid", employee.CabinetId);
                cmd.Parameters.AddWithValue("@user", employee.Username ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
        public int AddEquipment(Equipment equipment)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand(@"
            INSERT INTO Equipment 
                (Type, Model, OS, CabinetId) 
            VALUES 
                (@type, @model, @os, @cabinetId);
            SELECT last_insert_rowid();", conn);

                // Параметры с явной обработкой NULL
                cmd.Parameters.AddWithValue("@type", equipment.Type);
                cmd.Parameters.AddWithValue("@model", equipment.Model);
                cmd.Parameters.AddWithValue("@os", equipment.OS ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@cabinetId", equipment.CabinetId);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Получение пользователей для кабинета
        public List<Employees> GetEmployeesForCabinet(int cabinetId)
        {
            var employees = new List<Employees>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand(
                    "SELECT * FROM Employees WHERE CabinetId = @cabinetId",
                    connection);
                cmd.Parameters.AddWithValue("@cabinetId", cabinetId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(new Employees
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            FirstName = reader["FirstName"].ToString(),
                            LastName = reader["LastName"].ToString(),
                            Position = reader["Position"].ToString(),
                            CabinetId = Convert.ToInt32(reader["CabinetId"]),
                            Username = reader["Username"] is DBNull ? null : reader["Username"].ToString()
                        });
                    }
                }
            }
            return employees;
        }
        public List<string> GetUsersForCabinet(int cabinetId)
        {
            var users = new List<string>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand(
                    "SELECT DISTINCT Username FROM Messages WHERE CabinetNumber = @cab",
                    connection);
                cmd.Parameters.AddWithValue("@cab", cabinetId.ToString());
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(reader["Username"].ToString());
                    }
                }
            }
            return users;
        }
        public List<Equipment> GetEquipmentForCabinet(int cabinetId)
        {
            var equipment = new List<Equipment>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand(
                    "SELECT * FROM Equipment WHERE CabinetId = @cabinetId",
                    conn);
                cmd.Parameters.AddWithValue("@cabinetId", cabinetId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        equipment.Add(new Equipment
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Type = reader["Type"].ToString(),
                            Model = reader["Model"].ToString(),
                            OS = reader["OS"] is DBNull ? null : reader["OS"].ToString(),
                            CabinetId = Convert.ToInt32(reader["CabinetId"])
                        });
                    }
                }
            }
            return equipment;
        }

        public Cabinet GetCabinetById(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand("SELECT * FROM Cabinets WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Cabinet
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Number = reader["Number"].ToString(),
                            Description = reader["Description"].ToString()
                        };
                    }
                }
            }
            return null;
        }
        public List<string> GetAllUsers()
        {
            var usernames = new List<string>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand("SELECT Username FROM Users", connection);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        usernames.Add(reader["Username"].ToString());
                    }
                }
            }
            return usernames;
        }
        public Employees GetEmployeeById(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    "SELECT * FROM Employees WHERE Id = @id",
                    connection);
                command.Parameters.AddWithValue("@id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Employees
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            FirstName = reader["FirstName"].ToString(),
                            LastName = reader["LastName"].ToString(),
                            Position = reader["Position"].ToString(),
                            Username = reader["Username"].ToString(),
                            CabinetId = Convert.ToInt32(reader["CabinetId"])
                        };
                    }
                }
            }
            return null;
        }
        public void UpdateEmployee(Employees employee)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand(
                    @"UPDATE Employees SET 
                    FirstName = @fn,
                    LastName = @ln,
                    Position = @pos,
                    Username = @user
                WHERE Id = @id", connection);

                cmd.Parameters.AddWithValue("@fn", employee.FirstName);
                cmd.Parameters.AddWithValue("@ln", employee.LastName);
                cmd.Parameters.AddWithValue("@pos", employee.Position);
                cmd.Parameters.AddWithValue("@user", employee.Username ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", employee.Id);
                cmd.ExecuteNonQuery();
            }
        }
        public Equipment GetEquipmentById(int id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand(@"
            SELECT 
                Id, Type, Model, OS, CabinetId
            FROM Equipment 
            WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Equipment
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Type = reader["Type"].ToString(),
                            Model = reader["Model"].ToString(),
                            OS = reader["OS"] is DBNull ? null : reader["OS"].ToString(),
                            CabinetId = Convert.ToInt32(reader["CabinetId"])
                        };
                    }
                    return null;
                }
            }
        }
        public void UpdateEquipment(Equipment equipment)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand(@"
            UPDATE Equipment 
            SET 
                Type = @type,
                Model = @model,
                OS = @os
            WHERE Id = @id", conn);

                cmd.Parameters.AddWithValue("@type", equipment.Type);
                cmd.Parameters.AddWithValue("@model", equipment.Model);
                cmd.Parameters.AddWithValue("@os", equipment.OS ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", equipment.Id);

                cmd.ExecuteNonQuery();
            }
        }
        public void DeleteDatabase()
        {
            try
            {
                SQLiteConnection.ClearAllPools(); // Закрыть все соединения
                if (File.Exists(DbPath))
                {
                    File.Delete(DbPath); // Удалить файл базы
                }
                InitializeDatabase(); // Пересоздать структуру
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DELETE DATABASE ERROR] {ex}");
                throw;
            }
        }
        public void DeleteEmployee(int employeeId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Удаляем связанные записи из Equipment (если оборудование привязано к сотруднику)
                        var deleteEquipmentCmd = connection.CreateCommand();
                        deleteEquipmentCmd.CommandText = "DELETE FROM Equipment WHERE EmployeeId = @employeeId";
                        deleteEquipmentCmd.Parameters.AddWithValue("@employeeId", employeeId);
                        deleteEquipmentCmd.ExecuteNonQuery();

                        // Удаляем самого сотрудника
                        var deleteEmployeeCmd = connection.CreateCommand();
                        deleteEmployeeCmd.CommandText = "DELETE FROM Employees WHERE Id = @id";
                        deleteEmployeeCmd.Parameters.AddWithValue("@id", employeeId);
                        deleteEmployeeCmd.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void DeleteEquipment(int equipmentId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Equipment WHERE Id = @id";
                    command.Parameters.AddWithValue("@id", equipmentId);
                    command.ExecuteNonQuery();
                }
            }
        }
        public void DeleteCabinet(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("DELETE FROM Cabinets WHERE Id = @id", connection);
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
        }
    }
}
