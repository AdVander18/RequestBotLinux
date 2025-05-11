using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Data.SQLite;
using RequestBotLinux.Models;
using Avalonia.Controls;

namespace RequestBotLinux
{
    public class DataBase
    {
        public event Action MessageAdded;
        private readonly string _connectionString;
        public DataBase(string dbPath)
        {
            try
            {
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

                // Сохраняем пользователя
                var userCommand = new SQLiteCommand(
                    @"INSERT OR REPLACE INTO Users 
    (Username, FirstName, LastName) 
    VALUES (@username, @firstName, @lastName)",
                    connection);
                userCommand.Parameters.AddWithValue("@username", user.Username ?? "");
                userCommand.Parameters.AddWithValue("@firstName", user.FirstName ?? "");
                userCommand.Parameters.AddWithValue("@lastName", lastName);
                await userCommand.ExecuteNonQueryAsync();

                // Сохраняем задачу с дедлайном
                var messageCommand = new SQLiteCommand(
            @"INSERT INTO Messages 
    (Username, ChatId, MessageText, IsTask, Status, LastName, CabinetNumber, Deadline, Timestamp) 
    VALUES (@username, @chatId, @messageText, 1, 'Не завершено', @lastName, @cabinet, @deadline, @timestamp)",
            connection);

                messageCommand.Parameters.AddWithValue("@username", user.Username ?? "");
                messageCommand.Parameters.AddWithValue("@chatId", chatId);
                messageCommand.Parameters.AddWithValue("@messageText", description);
                messageCommand.Parameters.AddWithValue("@lastName", lastName);
                messageCommand.Parameters.AddWithValue("@cabinet", cabinet);
                messageCommand.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                // Конвертируем дедлайн в UTC
                messageCommand.Parameters.AddWithValue("@deadline", deadline.ToString("yyyy-MM-dd HH:mm:ss"));

                await messageCommand.ExecuteNonQueryAsync();
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
                    @"SELECT M.Id,
        M.Username,
        M.MessageText,
        M.Status, 
        M.LastName,
        M.CabinetNumber,
        U.FirstName,
        M.Timestamp,
        M.Deadline
  FROM Messages M
  LEFT JOIN Users U ON M.Username = U.Username
  WHERE M.IsTask = 1", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add(new TaskData
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Username = reader["Username"].ToString(),
                            MessageText = reader["MessageText"].ToString(),
                            Status = reader["Status"].ToString(),
                            FirstName = reader["FirstName"].ToString(),
                            LastName = reader["LastName"].ToString(),
                            CabinetNumber = reader["CabinetNumber"].ToString(),
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
        public async Task AddMessageAsync(string username, long chatId, string messageText)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SQLiteCommand(
                    "INSERT INTO Messages (Username, ChatId, MessageText) VALUES (@username, @chatId, @messageText)",
                    connection);

                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@chatId", chatId);
                command.Parameters.AddWithValue("@messageText", messageText);

                await command.ExecuteNonQueryAsync();
            }
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
        Timestamp, 
        IsFromAdmin,
        Username
      FROM Messages 
      WHERE Username = @username",
                    connection);

                command.Parameters.AddWithValue("@username", username);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        messages.Add(new MessageData
                        {
                            Username = reader["Username"].ToString(),
                            Text = reader["MessageText"].ToString(),
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

    }
}
