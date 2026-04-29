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
            IsChecked INTEGER DEFAULT 0,
            Deadline TEXT,
            TaskType TEXT,
            Priority INTEGER,
            EstimatedHours REAL,
            ParentId INTEGER
        );";
            command.ExecuteNonQuery();

            TryAddColumn(connection, "IsChecked", "INTEGER DEFAULT 0");
            TryAddColumn(connection, "ParentId", "INTEGER");
        }

        private void TryAddColumn(SqliteConnection connection, string column, string type)
        {
            try
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = $"ALTER TABLE Tasks ADD COLUMN {column} {type}";
                cmd.ExecuteNonQuery();
            }
            catch {  }
        }

        public List<TaskItem> LoadTasks()
        {
            var list = new List<TaskItem>();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Description, IsChecked, Deadline, TaskType, Priority, EstimatedHours, ParentId FROM Tasks";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TaskItem
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    IsChecked = reader.GetInt32(3) == 1,
                    Deadline = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                    TaskType = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Priority = reader.GetInt32(6),
                    EstimatedHours = reader.IsDBNull(7) ? 0.0 : reader.GetDouble(7),
                    ParentId = reader.IsDBNull(8) ? null : reader.GetInt32(8)
                });
            }

            var roots = list.Where(t => t.ParentId == null).ToList();
            var children = list.Where(t => t.ParentId != null).ToList();

            foreach (var child in children)
            {
                var parent = roots.FirstOrDefault(r => r.Id == child.ParentId);
                if (parent != null)
                    parent.SubTasks.Add(child);
            }

            return roots;
        }

        public void AddTask(TaskItem task)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
        INSERT INTO Tasks (Name, Description, IsChecked, Deadline, TaskType, Priority, EstimatedHours, ParentId)
        VALUES (@Name, @Description, @IsChecked, @Deadline, @TaskType, @Priority, @EstimatedHours, @ParentId);";

            command.Parameters.AddWithValue("@Name", task.Name);
            command.Parameters.AddWithValue("@Description", task.Description ?? "");
            command.Parameters.AddWithValue("@IsChecked", task.IsChecked ? 1 : 0);
            command.Parameters.AddWithValue("@Deadline", task.Deadline?.ToString("o") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@TaskType", task.TaskType ?? "");
            command.Parameters.AddWithValue("@Priority", task.Priority);
            command.Parameters.AddWithValue("@EstimatedHours", task.EstimatedHours);
            command.Parameters.AddWithValue("@ParentId", task.ParentId.HasValue ? task.ParentId.Value : DBNull.Value);
            command.ExecuteNonQuery();

            var idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            task.Id = Convert.ToInt32((long)idCmd.ExecuteScalar());
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
            IsChecked = @IsChecked,
            Deadline = @Deadline,
            TaskType = @TaskType,
            Priority = @Priority,
            EstimatedHours = @EstimatedHours,
            ParentId = @ParentId
        WHERE Id = @Id";

            command.Parameters.AddWithValue("@Name", task.Name);
            command.Parameters.AddWithValue("@Description", task.Description ?? "");
            command.Parameters.AddWithValue("@IsChecked", task.IsChecked ? 1 : 0);
            command.Parameters.AddWithValue("@Deadline", task.Deadline?.ToString("o") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@TaskType", task.TaskType ?? "");
            command.Parameters.AddWithValue("@Priority", task.Priority);
            command.Parameters.AddWithValue("@EstimatedHours", task.EstimatedHours);
            command.Parameters.AddWithValue("@ParentId", task.ParentId.HasValue ? task.ParentId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Id", task.Id);
            command.ExecuteNonQuery();
        }

        public void DeleteTask(int id)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Tasks WHERE Id=@Id OR ParentId=@Id";
            command.Parameters.AddWithValue("@Id", id);
            command.ExecuteNonQuery();
        }
    }
}