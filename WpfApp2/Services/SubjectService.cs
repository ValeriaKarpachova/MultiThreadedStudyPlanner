using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace WpfApp2.Services
{
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#534AB7";
        public override string ToString() => Name;
    }

    public class SubjectService
    {
        private const string Conn = "Data Source=tasks.db";

        public SubjectService()
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Subjects(
                    Id    INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name  TEXT NOT NULL,
                    Color TEXT NOT NULL DEFAULT '#534AB7');
                CREATE TABLE IF NOT EXISTS _SubjectMig(done INTEGER);
            ";
            cmd.ExecuteNonQuery();
            TryAddColumn(c, "Tasks", "SubjectId", "INTEGER");
        }

        private static void TryAddColumn(SqliteConnection c,
            string table, string col, string type)
        {
            try
            {
                var cmd = c.CreateCommand();
                cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {col} {type}";
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public List<Subject> GetAll()
        {
            var list = new List<Subject>();
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id,Name,Color FROM Subjects ORDER BY Name";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Subject
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Color = r.GetString(2)
                });
            return list;
        }

        public int Add(string name, string color = "#534AB7")
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO Subjects(Name,Color) VALUES(@n,@col)";
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@col", color);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            return (int)(long)cmd.ExecuteScalar()!;
        }

        public void Delete(int id)
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "DELETE FROM Subjects WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }
}