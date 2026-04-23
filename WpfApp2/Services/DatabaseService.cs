using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace WpfApp2.Services
{
    public class DatabaseService
    {
        private string connectionString = "Data Source=tasks.db";

        public void InitializeDatabase()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Tasks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Progress INTEGER,
                    Deadline TEXT,
                    TaskType TEXT,
                    Priority INTEGER,
                    EstimatedHours REAL
                );";

            command.ExecuteNonQuery();
        }

        public List<TaskItem> LoadTasks()
        {
            var list = new List<TaskItem>();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Tasks";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new TaskItem
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Progress = reader.GetInt32(3),
                    Deadline = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                    TaskType = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Priority = reader.GetInt32(6),
                    EstimatedHours = reader.IsDBNull(7) ? 0.0 : reader.GetDouble(7)
                });
            }

            return list;
        }

        public void AddTask(TaskItem task)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Tasks 
                (Name, Description, Progress, Deadline, TaskType, Priority, EstimatedHours)
                VALUES
                (@Name, @Description, @Progress, @Deadline, @TaskType, @Priority, @EstimatedHours);";

            command.Parameters.AddWithValue("@Name", task.Name);
            command.Parameters.AddWithValue("@Description", task.Description ?? "");
            command.Parameters.AddWithValue("@Progress", task.Progress);
            command.Parameters.AddWithValue("@Deadline", task.Deadline.HasValue? task.Deadline.Value.ToString("o") : DBNull.Value);
            command.Parameters.AddWithValue("@TaskType", task.TaskType ?? "");
            command.Parameters.AddWithValue("@Priority", task.Priority);
            command.Parameters.AddWithValue("@EstimatedHours", task.EstimatedHours);

            command.ExecuteNonQuery();

            var idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            task.Id = Convert.ToInt32((long)idCmd.ExecuteScalar());
        }

        public void DeleteTask(int id)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Tasks WHERE Id=@Id";
            command.Parameters.AddWithValue("@Id", id);

            command.ExecuteNonQuery();
        }

        public void UpdateTask(TaskItem task)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Tasks SET
                    Name = @Name,
                    Description = @Description,
                    Progress = @Progress,
                    Deadline = @Deadline,
                    TaskType = @TaskType,
                    Priority = @Priority,
                    EstimatedHours = @EstimatedHours
                WHERE Id = @Id";

            command.Parameters.AddWithValue("@Name", task.Name);
            command.Parameters.AddWithValue("@Description", task.Description ?? "");
            command.Parameters.AddWithValue("@Progress", task.Progress);
            command.Parameters.AddWithValue("@Deadline", task.Deadline?.ToString("o"));
            command.Parameters.AddWithValue("@TaskType", task.TaskType ?? "");
            command.Parameters.AddWithValue("@Priority", task.Priority);
            command.Parameters.AddWithValue("@EstimatedHours", task.EstimatedHours);
            command.Parameters.AddWithValue("@Id", task.Id);

            command.ExecuteNonQuery();
        }
    }
}