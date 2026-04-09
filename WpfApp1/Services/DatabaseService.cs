using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace WpfApp1.Services
{
    public class DatabaseService
    {
        private string connectionString = "Data Source=tasks.db;Version=3;";

        public void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = @"
                CREATE TABLE IF NOT EXISTS Tasks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Progress INTEGER,
                    Deadline DATETIME,
                    TaskType TEXT,
                    IsCompleted INTEGER,
                    Priority INTEGER
                );";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddTask(TaskItem task)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = @"
                INSERT INTO Tasks 
                (Name, Description, Progress, Deadline, TaskType, IsCompleted, Priority)
                VALUES
                (@Name, @Description, @Progress, @Deadline, @TaskType, @IsCompleted, @Priority);
                ";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Name", task.Name);
                    cmd.Parameters.AddWithValue("@Description", task.Description);
                    cmd.Parameters.AddWithValue("@Progress", task.Progress);
                    cmd.Parameters.AddWithValue("@Deadline", task.Deadline);
                    cmd.Parameters.AddWithValue("@TaskType", task.TaskType);
                    cmd.Parameters.AddWithValue("@IsCompleted", task.IsCompleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@Priority", task.Priority);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateTask(TaskItem task)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = @"
                UPDATE Tasks SET
                    Name = @Name,
                    Description = @Description,
                    Progress = @Progress,
                    Deadline = @Deadline,
                    TaskType = @TaskType,
                    IsCompleted = @IsCompleted,
                    Priority = @Priority
                WHERE Id = @Id;
                ";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", task.Id);
                    cmd.Parameters.AddWithValue("@Name", task.Name);
                    cmd.Parameters.AddWithValue("@Description", task.Description);
                    cmd.Parameters.AddWithValue("@Progress", task.Progress);
                    cmd.Parameters.AddWithValue("@Deadline", task.Deadline);
                    cmd.Parameters.AddWithValue("@TaskType", task.TaskType);
                    cmd.Parameters.AddWithValue("@IsCompleted", task.IsCompleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@Priority", task.Priority);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteTask(int id)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "DELETE FROM Tasks WHERE Id = @Id";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<TaskItem> LoadTasks()
        {
            var list = new List<TaskItem>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Tasks";

                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new TaskItem
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            Progress = Convert.ToInt32(reader["Progress"]),
                            Deadline = reader["Deadline"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["Deadline"]),
                            TaskType = reader["TaskType"].ToString()
                        });
                    }
                }
            }

            return list;
        }
    }
}