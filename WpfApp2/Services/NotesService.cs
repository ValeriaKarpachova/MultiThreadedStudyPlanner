using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace WpfApp2.Services
{
    public class NoteItem
    {
        public int Id { get; set; }
        public string Content { get; set; } = "";
        public string Color { get; set; } = "#FFFDE7"; 
    }

    public class NotesService
    {
        private const string Conn = "Data Source=notes.db";

        public NotesService()
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Notes(
                Id      INTEGER PRIMARY KEY AUTOINCREMENT,
                Content TEXT    NOT NULL DEFAULT '',
                Color   TEXT    NOT NULL DEFAULT '#FFFDE7');";
            cmd.ExecuteNonQuery();
        }

        public List<NoteItem> LoadAll()
        {
            var list = new List<NoteItem>();
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id,Content,Color FROM Notes ORDER BY Id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new NoteItem
                {
                    Id = r.GetInt32(0),
                    Content = r.GetString(1),
                    Color = r.GetString(2)
                });
            return list;
        }

        public int Add(string content = "", string color = "#FFFDE7")
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO Notes(Content,Color) VALUES(@c,@col)";
            cmd.Parameters.AddWithValue("@c", content);
            cmd.Parameters.AddWithValue("@col", color);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            return (int)(long)cmd.ExecuteScalar()!;
        }

        public void Update(NoteItem note)
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Notes SET Content=@c,Color=@col WHERE Id=@id";
            cmd.Parameters.AddWithValue("@c", note.Content);
            cmd.Parameters.AddWithValue("@col", note.Color);
            cmd.Parameters.AddWithValue("@id", note.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var c = new SqliteConnection(Conn);
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "DELETE FROM Notes WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }
}