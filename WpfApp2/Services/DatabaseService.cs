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
                    DeadlineTime   TEXT,
                    TaskType       TEXT,
                    Priority       INTEGER,
                    EstimatedHours REAL,
                    ParentId       INTEGER,
                    SubjectId      INTEGER
                );";
            cmd.ExecuteNonQuery();

        }

        public List<TaskItem> LoadTasks()
        {
            var list = new List<TaskItem>();
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id, Name, Description, IsChecked,
                Deadline, DeadlineTime, TaskType, Priority, EstimatedHours,
                ParentId, SubjectId FROM Tasks";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var item = new TaskItem
                {
                    Id             = r.GetInt32(0),
                    Name           = r.GetString(1),
                    Description    = r.IsDBNull(2) ? "" : r.GetString(2),
                    IsChecked      = r.GetInt32(3) == 1,
                    TaskType       = r.IsDBNull(6) ? "" : r.GetString(6),
                    Priority       = r.GetInt32(7),
                    EstimatedHours = r.IsDBNull(8) ? 0.0 : r.GetDouble(8),
                    ParentId       = r.IsDBNull(9)  ? null : r.GetInt32(9),
                    SubjectId      = r.IsDBNull(10) ? null : r.GetInt32(10)
                };

                if (!r.IsDBNull(4))
                {
                    var raw = r.GetString(4);
                    if (DateTime.TryParseExact(
                            raw.Substring(0, Math.Min(10, raw.Length)),
                            "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out var d))
                        item.Deadline = d;
                }

                if (!r.IsDBNull(5))
                {
                    var timeRaw = r.GetString(5);
                    if (!string.IsNullOrWhiteSpace(timeRaw) &&
                        TimeSpan.TryParseExact(timeRaw, @"hh\:mm",
                            System.Globalization.CultureInfo.InvariantCulture, out var ts))
                        item.DeadlineTime = ts;
                }

                list.Add(item);
            }

            var roots    = list.Where(t => t.ParentId == null).ToList();
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
            cmd.Parameters.AddWithValue("@Chk",  t.IsChecked ? 1 : 0);
            cmd.Parameters.AddWithValue("@Dead", t.Deadline.HasValue
                ? t.Deadline.Value.Date.ToString("yyyy-MM-dd")
                : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DeadTime", !string.IsNullOrEmpty(t.DeadlineTimeString)
                ? t.DeadlineTimeString
                : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Type", t.TaskType ?? "");
            cmd.Parameters.AddWithValue("@Pri",  t.Priority);
            cmd.Parameters.AddWithValue("@Est",  t.EstimatedHours);
            cmd.Parameters.AddWithValue("@Par",  t.ParentId.HasValue
                ? t.ParentId.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Sub",  t.SubjectId.HasValue
                ? t.SubjectId.Value : (object)DBNull.Value);
        }

        public void AddTask(TaskItem task)
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO Tasks
                (Name,Description,IsChecked,Deadline,DeadlineTime,TaskType,
                 Priority,EstimatedHours,ParentId,SubjectId)
                VALUES(@Name,@Desc,@Chk,@Dead,@DeadTime,@Type,
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
                Deadline=@Dead, DeadlineTime=@DeadTime, TaskType=@Type,
                Priority=@Pri, EstimatedHours=@Est, ParentId=@Par, SubjectId=@Sub
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
