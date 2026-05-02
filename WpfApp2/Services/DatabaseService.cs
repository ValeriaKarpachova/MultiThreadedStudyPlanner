using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp2.Services
{
    public class DatabaseService
    {
        private const string Conn = "Data Source=tasks.db";

        public void InitializeDatabase()
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Tasks (
                    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name           TEXT    NOT NULL,
                    Description    TEXT,
                    IsChecked      INTEGER DEFAULT 0,
                    Deadline       TEXT,
                    TaskType       TEXT,
                    Priority       INTEGER,
                    EstimatedHours REAL,
                    ParentId       INTEGER,
                    SubjectId      INTEGER
                );";
            cmd.ExecuteNonQuery();

            TryAddColumn(c, "IsChecked", "INTEGER DEFAULT 0");
            TryAddColumn(c, "ParentId", "INTEGER");
            TryAddColumn(c, "SubjectId", "INTEGER");
        }

        private static void TryAddColumn(
            SqliteConnection c, string col, string type)
        {
            try
            {
                var cmd = c.CreateCommand();
                cmd.CommandText = $"ALTER TABLE Tasks ADD COLUMN {col} {type}";
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public List<TaskItem> LoadTasks()
        {
            var list = new List<TaskItem>();
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id, Name, Description, IsChecked,
                Deadline, TaskType, Priority, EstimatedHours,
                ParentId, SubjectId FROM Tasks";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new TaskItem
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Description = r.IsDBNull(2) ? "" : r.GetString(2),
                    IsChecked = r.GetInt32(3) == 1,
                    Deadline = r.IsDBNull(4) ? null
                        : DateTime.ParseExact(
                            r.GetString(4).Substring(0, Math.Min(10, r.GetString(4).Length)),
                            "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture),
                    TaskType = r.IsDBNull(5) ? "" : r.GetString(5),
                    Priority = r.GetInt32(6),
                    EstimatedHours = r.IsDBNull(7) ? 0.0 : r.GetDouble(7),
                    ParentId = r.IsDBNull(8) ? null : r.GetInt32(8),
                    SubjectId = r.IsDBNull(9) ? null : r.GetInt32(9)
                });

            var roots = list.Where(t => t.ParentId == null).ToList();
            var children = list.Where(t => t.ParentId != null).ToList();
            foreach (var ch in children)
            {
                var p = roots.FirstOrDefault(x => x.Id == ch.ParentId);
                p?.SubTasks.Add(ch);
            }
            return roots;
        }

        private static void BindTask(SqliteCommand cmd, TaskItem t)
        {
            cmd.Parameters.AddWithValue("@Name", t.Name);
            cmd.Parameters.AddWithValue("@Desc", t.Description ?? "");
            cmd.Parameters.AddWithValue("@Chk", t.IsChecked ? 1 : 0);
            cmd.Parameters.AddWithValue("@Dead", t.Deadline.HasValue
                ? t.Deadline.Value.Date.ToString("yyyy-MM-dd")
                : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Type", t.TaskType ?? "");
            cmd.Parameters.AddWithValue("@Pri", t.Priority);
            cmd.Parameters.AddWithValue("@Est", t.EstimatedHours);
            cmd.Parameters.AddWithValue("@Par", t.ParentId.HasValue
                ? t.ParentId.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Sub", t.SubjectId.HasValue
                ? t.SubjectId.Value : (object)DBNull.Value);
        }

        public void AddTask(TaskItem task)
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO Tasks
                (Name,Description,IsChecked,Deadline,TaskType,
                 Priority,EstimatedHours,ParentId,SubjectId)
                VALUES(@Name,@Desc,@Chk,@Dead,@Type,
                       @Pri,@Est,@Par,@Sub)";
            BindTask(cmd, task);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            task.Id = Convert.ToInt32((long)cmd.ExecuteScalar()!);
        }

        public void UpdateTask(TaskItem task)
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE Tasks SET
                Name=@Name, Description=@Desc, IsChecked=@Chk,
                Deadline=@Dead, TaskType=@Type, Priority=@Pri,
                EstimatedHours=@Est, ParentId=@Par, SubjectId=@Sub
                WHERE Id=@Id";
            BindTask(cmd, task);
            cmd.Parameters.AddWithValue("@Id", task.Id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteTask(int id)
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "DELETE FROM Tasks WHERE Id=@Id OR ParentId=@Id";
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteSubTask(int id)
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "DELETE FROM Tasks WHERE Id=@Id";
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }
    }
}